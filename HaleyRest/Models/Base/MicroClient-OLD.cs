using Haley.Enums;
using Haley.Utils;
using System;
using Haley.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Concurrent;
using Haley.Models;
using Trs =System.Timers;
using System.Web;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Haley.Models
{
    //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body

    /// <summary>
    /// A simple straightforward HTTPClient Wrapper.
    /// </summary>
    public sealed class MicroClient :RestBase, IClient
    {
        public HttpClient BaseClient { get; }
        public string Id { get; }
        public string BaseURI { get;}
        public string FriendlyName { get; }
        public ConcurrentDictionary<Type,JsonConverter> JsonConverters { get; }

        #region Attributes
        ILogger _logger;
        Func<HttpRequestMessage, Task<bool>> RequestvalidationCallBack;
        HttpClientHandler handler = new HttpClientHandler();
        static string boundary = "----CustomBoundary" + DateTime.Now.Ticks.ToString("x");
        //private object requestSemaphore = new object(); //DONOT USE LOCK OR MONITOR. IT DOESN'T WORK AS EXPECTED WITH ASYNC AWAIT. USE SEMAPHORESLIM
        SemaphoreSlim requestSemaphore = new SemaphoreSlim(1,1); //Only 1 request to be granted (for this client).
        Uri _base_uri;
        string request_token;
        ConcurrentDictionary<string, IEnumerable<string>> _requestHeaders = new ConcurrentDictionary<string, IEnumerable<string>>();
        CancellationToken cancellation_token = default(CancellationToken);
        bool add_cancellation_token = false;
        Trs.Timer semaphoreTimer = new Trs.Timer(15000) { AutoReset = false}; //15K milliseconds is 15 seconds.
        #endregion

        #region Constructors
        public MicroClient(string base_address,string friendly_name ,Func<HttpRequestMessage, Task<bool>> request_validationcallback,ILogger logger)
        {
            Id = Guid.NewGuid().ToString();
            BaseURI = base_address;
            _base_uri = getBaseUri(base_address);
            _logger = logger;
            JsonConverters = new ConcurrentDictionary<Type, JsonConverter>();
            RequestvalidationCallBack = request_validationcallback;
            if (string.IsNullOrWhiteSpace(friendly_name)) friendly_name = base_address;
            FriendlyName = friendly_name;
            if (_base_uri == null)
            {
                logger?.LogDebug($@"ERROR: Base URI is null. MicroClient cannot be created.");
                return;
            }
            BaseClient = new HttpClient(handler,false); //Base client is read only. So initiate only once.
            BaseClient.BaseAddress = _base_uri; //Address can be set only once. Calling multiple times will throw exception.
            ResetHeaders();
            semaphoreTimer.Elapsed += SemaPhoreTimer_Elapsed;
        }
       

        public MicroClient(Uri base_uri, string friendly_name, ILogger logger = null) : this(base_uri.AbsoluteUri, friendly_name,logger) { }
        public MicroClient(string base_uri, string friendly_name,ILogger logger = null) : this(base_uri, friendly_name,null,logger) { }
        public MicroClient(string base_uri, ILogger logger = null) : this(base_uri, base_uri, null,logger) { }

        #endregion

        #region FluentMethods

        public IClient SetLogger(ILogger logger) {
            _logger = logger;
            return this;
        }
        public IClient AddJsonConverters(JsonConverter converter)
        {
            try
            {
                if (converter == null) return this;

                if (!JsonConverters.ContainsKey(converter.GetType()))
                {
                    JsonConverters.TryAdd(converter.GetType(), converter);
                }
                return this;
            }
            catch (Exception ex)
            {
                EventId _eventid = new EventId(5001, "JSONConverter Add Error");
                WriteLog(LogLevel.Trace, _eventid, "Error while trying to JSON Converter", ex);
                return this;
            }
        }
        public IClient RemoveJsonConverters(JsonConverter converter)
        {
            try
            {
                if (converter == null) return this;
                var _type = converter.GetType();
                if (JsonConverters.ContainsKey(_type))
                {
                    JsonConverters.TryRemove(_type, out var removed);
                }
                return this;
            }
            catch (Exception)
            {
                return this;
            }
        }
        public IClient ResetHeaders()
        {
            WriteLog(LogLevel.Debug, "Resetting the client headers");
            //remains the same throught the life time of this client.
            //BaseClient.BaseAddress = _base_uri; //Base address cannot be reset multiple times.
            BaseClient.DefaultRequestHeaders.Accept.Clear();
            BaseClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            //BaseClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            return this;
        }
        public IClient ClearRequestHeaders()
        {
            _requestHeaders = new ConcurrentDictionary<string, IEnumerable<string>>(); //Clear the requestheaders.
            WriteLog(LogLevel.Debug, "Client request headers cleared");
            return this;
        }
        public IClient AddRequestHeaders(string name, string value)
        {
            _requestHeaders?.TryAdd(name, new List<string>() { value });
            return this;
        }
        public IClient AddRequestHeaders(string name, List<string> values)
        {
            _requestHeaders?.TryAdd(name, values);
            return this;
        }
        /// <summary>
        /// This authentication will NOT be added to the headers,as the client is re-used. This will be added to each request header (if authorization is requested).
        /// </summary>
        /// <param name="token"></param>
        /// <param name="token_prefix"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public IClient AddRequestAuthentication(string token, string token_prefix = "Bearer")
        {
            request_token = PrepareToken(token, token_prefix);
            return this;
        }
        public IClient ClearRequestAuthentication()
        {
            request_token = string.Empty;
            return this;
        }
        public IClient AddClientHeaderAuthentication(string token, string token_prefix = "Bearer")
        {
            ResetHeaders(); //Re initiate the client (clearing old headers)
            var _headerToken = PrepareToken(token, token_prefix);
            if (!string.IsNullOrWhiteSpace(_headerToken))
            {
                //If it is null, then do not set anything. However, it would have already been cleared.
                BaseClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _headerToken);
                //BaseClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_headerToken);
            }
            return this;
        }
        #endregion

        #region Get Methods
        public async Task<StringResponse> GetAsync(string resource_url) {
            return await GetAsync(resource_url, null);
        }
        public async Task<StringResponse> GetAsync(string resource_url, QueryParam parameter) {
            List<QueryParam> queries = new List<QueryParam>();
            queries.Add(parameter);
            return await GetByParamsAsync(resource_url, queries);
        }
        public async Task<StringResponse> GetByParamsAsync(string resource_url, IEnumerable<QueryParam> parameters) {
            return await GetByParamsAsync<string>(resource_url, parameters);
        }

        #endregion

        #region GetSerialized Methods
        public async Task<SerializedResponse<T>> GetAsync<T>(string resource_url) where T : class {
            return await GetAsync<T>(resource_url, null);
        }
        public async Task<SerializedResponse<T>> GetAsync<T>(string resource_url, QueryParam parameter) where T : class {
            List<QueryParam> queries = new List<QueryParam>();
            queries.Add(parameter);
            return await GetByParamsAsync<T>(resource_url, queries);
        }
        public async Task<SerializedResponse<T>> GetByParamsAsync<T>(string resource_url, IEnumerable<QueryParam> parameters) where T : class {
            var _response = await SendObjectsAsync(resource_url, parameters, Method.GET);
            SerializedResponse<T> result = new SerializedResponse<T>();
            //_response.CopyTo(result);
            _response.MapProperties(result);
            if (_response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(result.StringContent)) {
                try {
                    if (typeof(T) == typeof(string)) {
                        result.SerializedContent = result.StringContent as T;
                    } else {
                        result.SerializedContent = JsonSerializer.Deserialize<T>(result.StringContent);
                    }
                } catch (Exception) {
                    result.SerializedContent = null; //Since it is a class, it should be nullable.
                }
            }
            return result;
        }
        #endregion

        #region Post Methods
        public async Task<IResponse> PostObjectAsync(string resource_url, RequestObject parameter)
        {
            return await PostObjectsAsync(resource_url, new List<RequestObject>() { parameter });
        }
        public async Task<IResponse> PostObjectsAsync(string resource_url, IEnumerable<RequestObject> parameters)
        {
            return await SendObjectsAsync(resource_url, parameters, Method.POST);
        }
        #endregion

        #region Delete Methods
        public async Task<IResponse> DeleteObjectAsync(string resource_url, QueryParam param) {
            return await DeleteObjectsAsync(resource_url, new List<QueryParam>() { param });
        }
        public async Task<IResponse> DeleteObjectsAsync(string resource_url, IEnumerable<QueryParam> parameters) {
            return await SendObjectsAsync(resource_url, parameters, Method.DELETE);
        }
        #endregion

        #region Send Methods
        public async Task<IResponse> SendAsync(string url, object content, Method method, bool is_serialized, BodyContentType content_type = BodyContentType.StringContent)
        {
            return await SendObjectAsync(url, new RawBodyRequest(content, is_serialized, content_type),method);
        }
        public async Task<IResponse> SendObjectAsync(string url, RequestObject param, Method method)
        {
            //Just add this single param as a list to the send method.
            return await SendObjectsAsync(url, new List<RequestObject>() { param }, method);
        }
        public async Task<IResponse> SendObjectsAsync(string url, IEnumerable<RequestObject> paramList, Method method) {
            string inputURL = url;
            var processedInputs = ConverToHttpContent(inputURL, paramList, method); //Put required url queries, bodies etc.
            return await SendAsync(processedInputs.url, processedInputs.content, method);
        }
        public async Task<IResponse> SendAsync(string url, HttpContent content, Method method) {

            WriteLog(LogLevel.Information, $@"Initiating a {method} request to {url} with base url {BaseURI}");
            //1. Here, we do not add anything to the URL or Content.
            //2. We just validate the URl and get the path and query part.
            //3. Add request headers and Authentication (if available).
            HttpMethod request_method = HttpMethod.Get;
            switch (method) {
                case Method.GET:
                    request_method = HttpMethod.Get;
                    break;
                case Method.POST:
                    request_method = HttpMethod.Post;
                    break;
                case Method.DELETE:
                    request_method = HttpMethod.Delete;
                    break;
                case Method.PUT:
                    request_method = HttpMethod.Put;
                    break;
            }
            //At this point, do not parse the URL. It might already contain the URL params added to it. So just call the URL. // parseURI(url).resource_part
            var request = new HttpRequestMessage(request_method, parseURI(url).pathQuery); //URL should not have base part.

            if (content != null) request.Content = content; //Set content if not null

            //If the request has some kind of request headers, then add them.
            if (!string.IsNullOrWhiteSpace(request_token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue(request_token); //if the input is not correct, for instance, token has space, then it will throw exception. Add without validation.
                //request.Headers.TryAddWithoutValidation("Authorization", request_token);
            }

            //Add other request headers if available.
            if (_requestHeaders != null && _requestHeaders?.Count > 0) {
                foreach (var kvp in _requestHeaders) {
                    try {
                        request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value); //Do not validate.
                    } catch (Exception ex) {
                        WriteLog(LogLevel.Debug, new EventId(2001, "Header Error"), "Error while trying to add a header", ex);
                    }
                }
            }

            StringResponse result = new StringResponse();
            var _response = await SendAsync(request);
            _response.CopyTo(result); //Copy base value.
            //Response we receive will be base response.
            if (_response.IsSuccessStatusCode) {
                var _cntnt = _response.Content;
                var _strCntnt = await _cntnt.ReadAsStringAsync();
                result.StringContent = _strCntnt;

            }
            return result; //All calls from here will receive stringResponse content.
        }
        public async Task<IResponse> SendAsync(HttpRequestMessage request) {
            //if some sort of validation callback is assigned, then call that first.
            if (RequestvalidationCallBack != null) {
                var validation_check = await RequestvalidationCallBack.Invoke(request);
                if (!validation_check) {
                    WriteLog(LogLevel.Information, "Local request validation failed. Please verify the validation methods to return true on successful validation");
                    return new StringResponse() { StringContent = "Internal Request Validation call back failed." };
                }
            }

            //Here we donot modify anything. We just send and receive the response.
            HttpResponseMessage message;
            if (add_cancellation_token) {
                message = await BaseClient.SendAsync(request, cancellation_token);
                //After the token is added, we just remove it.
                cancellation_token = default(CancellationToken);
                add_cancellation_token = false;
            } else {
                message = await BaseClient.SendAsync(request);
            }

            ////After you have send the request, there is no need to block any other thread, since the private variables would have been consumed. So release them.
            //UnBlockClient();

            var _response = new BaseResponse() { OriginalResponse = message };
            return _response;
        }
        #endregion

        #region Implemented Methods
        public IClient AddRequestCancellationToken(CancellationToken token)
        {
            //Adds only for this request.
            cancellation_token = token;
            add_cancellation_token = true;
            return this;
        }
        public Dictionary<string, IEnumerable<string>> GetRequestHeaders()
        {
            try
            {
                return _requestHeaders?.ToDictionary(p => p.Key, p => p.Value);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        public override string ToString()
        {
            return this.FriendlyName;
        }
    }
}
