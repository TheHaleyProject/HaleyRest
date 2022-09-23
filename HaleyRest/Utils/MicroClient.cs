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

namespace Haley.Utils
{
    //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body

    /// <summary>
    /// A simple straightforward HTTPClient Wrapper.
    /// </summary>
    public sealed class MicroClient :IClient
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
            ResetClientHeaders();
            semaphoreTimer.Elapsed += SemaPhoreTimer_Elapsed;
        }
       

        public MicroClient(Uri base_uri, string friendly_name = null, ILogger logger = null) : this(base_uri.AbsoluteUri, friendly_name,logger) { }
        public MicroClient(string base_uri, string friendly_name = null,ILogger logger = null) : this(base_uri, friendly_name,null,logger) { }
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
        public IClient ResetClientHeaders()
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
            ResetClientHeaders(); //Re initiate the client (clearing old headers)
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
                //request.Headers.Authorization = new AuthenticationHeaderValue(request_token); //if the input is not correct, for instance, token has space, then it will throw exception. Add without validation.
                request.Headers.TryAddWithoutValidation("Authorization", request_token);
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

        //TODO : Enhance thread safe methods.
        #region ThreadSafe Implementation
        private void WriteBlockDebugMessage(string title,string message = null)
        {
            string towrite = $@"Microclient ==> {title}: Count : {requestSemaphore.CurrentCount} at  {DateTime.Now.ToLongTimeString()} for client {Id} with address {_base_uri}";
            if (!string.IsNullOrWhiteSpace(message))
            {
                towrite = towrite + $@" ===> {message}";
            }
            Debug.WriteLine(towrite);
        }
        private void WriteTimerDebugMessage(string title, string message = null)
        {
            string towrite = $@"Microclient ==> {title} with {semaphoreTimer.Interval} milliseconds : at {DateTime.Now.ToLongTimeString()} for client {Id} with address {_base_uri}";
            if (!string.IsNullOrWhiteSpace(message))
            {
                towrite = towrite + $@" ===> {message}";
            }
            Debug.WriteLine(towrite);
        }
        public IClient BlockClient(string message = null)
        {
            return BlockClient(0,message);
        }
        public IClient BlockClient(double block_seconds = 15, string message = null)
        {
            BlockClientAsync(block_seconds,message).Wait();
            return this; //Block and return this client. So no other thread can use until this is unblocked.
        }
        public async Task BlockClientAsync(string message = null)
        {
            await BlockClientAsync(0,message);
        }
        public async Task BlockClientAsync(double block_seconds = 15, string message = null)
        {
            WriteBlockDebugMessage("Waiting",message);
            await requestSemaphore.WaitAsync(); //All requests will wait here.
            WriteBlockDebugMessage("Blocked",message);
            semaphoreTimer.Stop(); //It it is running for someother reason.
            if (block_seconds > 0)
            {
                semaphoreTimer.Interval = block_seconds * 1000.0; //If the interval is 0 , then we donot start the timer. change seconds into milliseconds.
                WriteTimerDebugMessage("Timer Started",message);
                semaphoreTimer.Start(); //Star the timer.
            }
        }
        public IClient UnBlockClient(string message = null)
        {
            if (!requestSemaphore.Wait(0)) //Just to check if we are able to enter the current thread. 
            {
                if (semaphoreTimer.Enabled)
                {
                    semaphoreTimer.Stop(); //If we prematurely decide to Unblock the client, the timer can be stopped.
                    WriteTimerDebugMessage("Timer Stopped",message);
                }
                
                //If we are not able to enter inside then it means that we already have some other process going on inside. We just release it.
                requestSemaphore.Release();
                WriteBlockDebugMessage("Released", message);
            }
            return this;
        }
        #endregion

        #region Helpers
        private (HttpContent content, string url) ConverToHttpContent(string url, IEnumerable<RequestObject> paramList, Method method) {
            try {
                //HTTPCONENT itself is a abstract class. We can have StringContent, StreamContent,FormURLEncodedContent,MultiPartFormdataContent.
                //Based on the params, we might add the data to content or to the url (in case of get).
                if (paramList == null || paramList?.Count() == 0) return (null, url);
                HttpContent processed_content = null;
                string processed_url = url;

                //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body
                //A get request can have a content body.

                //The paramlist might containt multiple request param(which will be trasformed in to query). however, only one (the first) request body will be considered
                processed_content = PrepareBody(paramList, method);
                processed_url = PrepareQuery(url, paramList);
                return (processed_content, processed_url);
            } catch (Exception ex) {
                throw ex;
            }
        }
        private string LogFormatter (string state,Exception exception)
        {
            if (exception == null) return state;
            return (state + Environment.NewLine+ exception.Message.ToString() + Environment.NewLine + exception.StackTrace.ToString());
        }

        private void WriteLog(LogLevel level, EventId evtId, string msg, Exception ex) {
            _logger?.Log(level, evtId, msg, ex, LogFormatter);
        }

        private void WriteLog(LogLevel level,string msg) {
            _logger?.Log(level, msg);
        }
        private void SemaPhoreTimer_Elapsed(object sender, Trs.ElapsedEventArgs e)
        {
            WriteTimerDebugMessage("Timer Elapsed", "Elapsed call.");
            UnBlockClient("Elapsed Call");
        }
        private string PrepareToken(string token, string token_prefix)
        {
            try
            {
                var _token = string.Concat(token_prefix ?? "", " ", token);
                return _token?.Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }
        private HttpContent PrepareBody(IEnumerable<RequestObject> paramList, Method method)
        {
            //We can add only one type of body to an object. If we have more than one type, we log the error and take only the first item.
            try
            {
                HttpContent result = null;
                //paramList.Where(p=> typeof(IRequestBody).IsAssignableFrom(p))?.f
                var _requestBody = paramList.Where(p => p is IRequestBody)?.FirstOrDefault();
                if (_requestBody == null || _requestBody.Value == null) return result; //Not need of further processing for null values.

                if(_requestBody is RawBodyRequest rawReq) {
                    //Just add a raw content and send.
                    result = prepareRawBody(rawReq);

                } else if (_requestBody is FormBodyRequest formreq) {
                    //Decide if this is multipart form or urlencoded form data
                    result = prepareFormBody(formreq);
                }
                return result;
            }
            catch (Exception ex)
            {
                WriteLog(LogLevel.Trace, new EventId(6000), "Error while trying to prepare body", ex);
                return null;
            }
        }
        private string PrepareQuery(string url, IEnumerable<RequestObject> paramList)
        {
            string result = url;
            var _query = HttpUtility.ParseQueryString(string.Empty);

            var _paramQueries = paramList.Where(p => p is IRequestQuery)?.Cast<IRequestQuery>().ToList();
            if (_paramQueries == null || _paramQueries.Count == 0) return result; //return the input url

            foreach (var param in _paramQueries)
            {
                var _key = param.Key;
                var _value = param.Value;

                if (param.ShouldEncode) {
                    //Encode before adding
                    if (!param.IsEncoded) {
                        _key = Uri.EscapeDataString(_key);
                        _value = Uri.EscapeDataString(_value);
                        param.SetEncoded();
                    }
                }
                _query[_key] = _value;
            }

            var _formed_query = _query.ToString();
            if(!string.IsNullOrWhiteSpace(_formed_query))
            {
                result = result + "?" + _formed_query;
            }
            return result;
        }
        private HttpContent prepareRawBody(RawBodyRequest rawbody) {
            try {
                HttpContent result = null;
                switch (rawbody.BodyType) {
                    case BodyContentType.StringContent:
                        string mediatype = null;
                        string _serialized_content = rawbody.Value as string; //Assuming it is already serialized.

                        switch (rawbody.StringBodyFormat) {
                            case StringContentFormat.Json:
                                if (!rawbody.IsSerialized) {
                                    _serialized_content = rawbody.ToJson(JsonConverters?.Values?.ToList());
                                }
                                mediatype = "application/json";
                                break;

                            case StringContentFormat.XML:
                                if (!rawbody.IsSerialized) {
                                    _serialized_content = rawbody.ToXml().ToString();
                                }
                                mediatype = "application/xml";
                                break;
                            case StringContentFormat.PlainText:
                                if (!rawbody.IsSerialized) {
                                    _serialized_content = rawbody.ToJson(JsonConverters?.Values?.ToList());
                                }
                                mediatype = "text/plain";
                                break;
                        }
                        result = new StringContent(_serialized_content, Encoding.UTF8, mediatype);
                        break;

                    case BodyContentType.ByteArrayContent:
                    case BodyContentType.StreamContent:
                        if (rawbody.Value is byte[] byteContent) {
                            //If byte content.
                            result = new ByteArrayContent(byteContent, 0, byteContent.Length);
                        } else if (rawbody.Value is Stream streamContent) {
                            //If stream content.
                            result = new StreamContent(streamContent);
                            //Dont' remove all headers. Only the content type. Header might have authentications properly set.
                            result.Headers.Remove("Content-Type");
                            result.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            result.Headers.ContentDisposition = new ContentDispositionHeaderValue("stream-data") { FileName = rawbody.FileName ?? "attachment" };
                        } 
                        break;
                }
                return result;
            } catch (Exception ex) {
               WriteLog(LogLevel.Trace, new EventId(6001), "Error while trying to prepare Raw body", ex);
                return null;
            }
        }
        private HttpContent prepareFormBody(FormBodyRequest formbody) {
            try {
                HttpContent result = null;
                //Form can be url encoded form and multi form.. //TODO : REFINE
                //For more than one add as form data.
                MultipartFormDataContent form_content = new MultipartFormDataContent();
                form_content.Headers.Remove("Content-Type");
                form_content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);

                foreach (var item in formbody.Value) {
                    if (item.Value == null) continue;
                    var rawContent = prepareRawBody(item.Value);
                    if (string.IsNullOrWhiteSpace(item.Value.FileName)) {
                        form_content.Add(rawContent, item.Key); //Also add the key.
                    } else {
                        form_content.Add(rawContent, item.Key, item.Value.FileName); //File name cannot be empty. Sending empty variable throws exception/
                    }
                }

                return result;
            } catch (Exception ex) {
               WriteLog(LogLevel.Trace, new EventId(6002), "Error while trying to prepare Form body", ex);
                return null;
            }
        }
        private (string authority, string pathQuery) parseURI(string input_url)
        {
            try
            {
                if (string.IsNullOrEmpty(input_url)) return (null, null);
                if(Uri.TryCreate(input_url,UriKind.RelativeOrAbsolute,out Uri _uri))
                {
                    if (_uri.IsAbsoluteUri)
                    {
                        string _authority = _uri.GetLeftPart(UriPartial.Authority);
                        string _method = input_url.Substring(_authority.Length);
                        return (_authority, _method);
                    }
                }
                return (null, input_url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: " + ex.StackTrace);
                return (null, input_url);
            }
        }
        private (string authority, string pathQuery) parseURI(Uri input_uri)
        {
            return parseURI(input_uri.AbsoluteUri);
        }
        private Uri getBaseUri(string address)
        {
            bool result = Uri.TryCreate(address, UriKind.Absolute, out var uri_result)
                && (uri_result.Scheme == Uri.UriSchemeHttp || uri_result.Scheme == Uri.UriSchemeHttps);
            if (result) return uri_result;
            Console.WriteLine($@"ERROR: Unable to create URI from the address {address}");
            return null;
        }
        private Uri getBaseUri(Uri inputURI)
        {
            return getBaseUri(inputURI.AbsoluteUri);
        }
        #endregion
        public override string ToString()
        {
            return this.FriendlyName;
        }
    }
}
