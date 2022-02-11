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

namespace Haley.Utils
{
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
        public ILogger Logger { get; }

        #region Attributes
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
            Logger = logger;
            JsonConverters = new ConcurrentDictionary<Type, JsonConverter>();
            RequestvalidationCallBack = request_validationcallback;
            if (string.IsNullOrWhiteSpace(friendly_name)) friendly_name = base_address;
            FriendlyName = friendly_name;
            if (_base_uri == null)
            {
                Debug.WriteLine($@"ERROR: Base URI is null. MicroClient cannot be created.");
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
                Logger?.Log(LogLevel.Trace, _eventid, "Error while trying to JSON Converter", ex, LogFormatter);
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
            request_token = _getJWT(token, token_prefix);
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
            var _headerToken = _getJWT(token, token_prefix);
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
        public async Task<SerializedResponse<T>> GetAsync<T>(string resource_url) where T : class
        {
            return await GetAsync<T>(resource_url, null);
        }
        public async Task<StringResponse> GetAsync(string resource_url)
        {
            return await GetAsync(resource_url,null);
        }

        public async Task<SerializedResponse<T>> GetAsync<T>(string resource_url, string id_parameter) where T : class
        {
            if (!string.IsNullOrWhiteSpace(id_parameter))
            {
                Dictionary<string, string> _requestDic = new Dictionary<string, string>();
                _requestDic.Add("id", id_parameter);
                return await GetByDictionaryAsync<T>(resource_url, _requestDic);
            }
            else
            {
                return await GetAsync<T>(resource_url, null);
            }
        }
        public async Task<StringResponse> GetAsync(string resource_url, string id_parameter)
        {
            if (!string.IsNullOrWhiteSpace(id_parameter))
            {
                Dictionary<string, string> _requestDic = new Dictionary<string, string>();
                _requestDic.Add("id", id_parameter);
                return await GetByDictionaryAsync(resource_url, _requestDic);
            }
            else
            {
                return await GetAsync(resource_url, null);
            }
        }

        public async Task<StringResponse> GetByDictionaryAsync(string resource_url, Dictionary<string, string> parameters)
        {
            return await GetByDictionaryAsync<string>(resource_url, parameters);
        }
        public async Task<SerializedResponse<T>> GetByDictionaryAsync<T>(string resource_url, Dictionary<string, string> parameters) where T : class
        {
            List<RestParam> paramslist = new List<RestParam>();
            if (parameters != null && parameters?.Count > 0)
            {
                foreach (var kvp in parameters)
                {
                    // For get, all the entries are in query string. Since we get the dictionary as string, we don't need serialization.
                    paramslist.Add(new RestParam(kvp.Key, kvp.Value, true, ParamType.QueryString));
                }
            }

            var _response = await SendAsync(resource_url, paramslist, Method.Get);
            SerializedResponse<T> result = new SerializedResponse<T>();
            //_response.CopyTo(result);
            _response.MapProperties(result);
            if (_response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(result.StringContent))
            {
                try
                {
                    if (typeof(T) == typeof(string))
                    {
                        result.SerializedContent = result.StringContent as T;
                    }
                    else
                    {
                        result.SerializedContent = JsonSerializer.Deserialize<T>(result.StringContent);
                    }
                }
                catch (Exception)
                {
                    result.SerializedContent = null; //Since it is a class, it should be nullable.
                }
            }
            return result;
        }

        #endregion

        #region Post Methods
        public async Task<IResponse> PostDictionaryAsync(string resource_url, Dictionary<string, string> dictionary)
        {
            //When we directly post dictionary of string as parameters, we just try to seriazlie them to string.
            return await PostObjectAsync(resource_url, dictionary.ToJson(), true); //parameters are in Dictionary<string,string> format so it will be direclty serizlied without need for any converter.
        }
        public async Task<IResponse> PostObjectAsync(string resource_url, object content, bool is_serialized = false) 
        {
            return await PostAsync(resource_url, new RestParam("id", content, is_serialized, ParamType.RequestBody));
        }
        public async Task<IResponse> PostAsync(string resource_url, RestParam param)
        {
            return await PostAsync(resource_url, new List<RestParam>() { param });
        }
        public async Task<IResponse> PostAsync(string resource_url, IEnumerable<RestParam> param_list)
        {
            return await SendAsync(resource_url, paramList: param_list, Method.Post);
        }
        #endregion

        #region Send Methods
        public async Task<IResponse> SendAsync(string url, object content, Method method = Method.Get, ParamType param_type = ParamType.Default, bool is_serialized = false)
        {
            return await SendAsync(url, new RestParam("id", content, is_serialized, param_type), method);
        }
        public async Task<IResponse> SendAsync(string url, RestParam param, Method method = Method.Get)
        {
            //Just add this single param as a list to the send method.
            return await SendAsync(url, new List<RestParam>() { param }, method);
        }
        #endregion

        #region Main calls
        public async Task<IResponse> SendAsync(string url, IEnumerable<RestParam> paramList, Method method = Method.Get)
        {
            string inputURL = url;
            processParamTypes(ref paramList, method);
            var processedInputs = processInputs(inputURL, paramList, method);
            return await SendAsync(processedInputs.url, processedInputs.content, method);
        }
        public async Task<IResponse> SendAsync(string url, HttpContent content, Method method = Method.Get)
        {
            //1. Here, we do not add anything to the URL or Content.
            //2. We just validate the URl and get the path and query part.
            //3. Add request headers and Authentication (if available).
            HttpMethod request_method = HttpMethod.Get;
            switch (method)
            {
                case Method.Get:
                    request_method = HttpMethod.Get;
                    break;
                case Method.Post:
                    request_method = HttpMethod.Post;
                    break;
                case Method.Delete:
                    request_method = HttpMethod.Delete;
                    break;
                case Method.Update:
                    request_method = HttpMethod.Put;
                    break;
            }
            //At this point, do not parse the URL. It might already contain the URL params added to it. So just call the URL. // parseURI(url).resource_part
            var request = new HttpRequestMessage(request_method, parseURI(url).pathQuery) { Content = content }; //URL should not have base part.

            //If the request has some kind of request headers, then add them.
            if (!string.IsNullOrWhiteSpace(request_token))
            {
                //request.Headers.Authorization = new AuthenticationHeaderValue(request_token); //if the input is not correct, for instance, token has space, then it will throw exception. Add without validation.
                request.Headers.TryAddWithoutValidation("Authorization", request_token);
            }

            //Add other request headers if available.
            if (_requestHeaders != null && _requestHeaders?.Count > 0)
            {
                foreach (var kvp in _requestHeaders)
                {
                    try
                    {
                        request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value); //Do not validate.
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
            }

            StringResponse result = new StringResponse();
            var _response = await SendAsync(request);
            _response.CopyTo(result); //Copy base value.
            //Response we receive will be base response.
            if (_response.IsSuccessStatusCode)
            {
                var _cntnt = _response.Content;
                var _strCntnt = await _cntnt.ReadAsStringAsync();
                result.StringContent = _strCntnt;

            }
            return result; //All calls from here will receive stringResponse content.
        }
        public async Task<IResponse> SendAsync(HttpRequestMessage request)
        {
            //if some sort of validation callback is assigned, then call that first.
            if (RequestvalidationCallBack != null)
            {
                var validation_check = await RequestvalidationCallBack.Invoke(request);
                if (!validation_check)
                {
                    return new StringResponse() {StringContent = "Request Validation call back failed." }; 
                }
            }

            //Here we donot modify anything. We just send and receive the response.

            HttpResponseMessage message;
            if (add_cancellation_token)
            {
                message = await BaseClient.SendAsync(request, cancellation_token);
                //After the token is added, we just remove it.
                cancellation_token = default(CancellationToken);
                add_cancellation_token = false;
            }
            else
            {
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
        private string  LogFormatter (string state,Exception exception)
        {
            if (exception == null) return state;
            return (state + Environment.NewLine+ exception.Message.ToString() + Environment.NewLine + exception.StackTrace.ToString());
        }
        private void SemaPhoreTimer_Elapsed(object sender, Trs.ElapsedEventArgs e)
        {
            WriteTimerDebugMessage("Timer Elapsed", "Elapsed call.");
            UnBlockClient("Elapsed Call");
        }
        private void processParamTypes(ref IEnumerable<RestParam> @params,Method method)
        {
            try
            {
                if (@params == null || @params?.Count() == 0) return;

                //For delete, post, put, we can have, data in both query string and also in request body.
                //For get, the data should only be in the query string. So, remove all the params except the request body.

                //CHANGE PARAMTYPE ONLY IF ITS NOT SET DIRECTLY. for instance, a POST can still have query and GET can still have Request body. This will be handled when trying to create the content.
                //if paramtype is default, then we replace them with relevant types.
                @params.Where(p=>p.ParamType == ParamType.Default)?.ToList().ForEach(q =>
                {
                    switch (method)
                    {
                        case Method.Post:
                        case Method.Delete:
                        case Method.Update:
                            q.ParamType = ParamType.RequestBody; //if post, we then set the parameter as body
                            break;
                        case Method.Get:
                            q.ParamType = ParamType.QueryString; //if post, we then set the parameter as body
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        private string _getJWT(string token, string token_prefix)
        {
            try
            {
                var _token = token_prefix ?? "";
                _token = _token + " " + token;
                return _token?.Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }
        private HttpContent _createContent(RestParam param)
        {
            try
            {
                HttpContent result = null;
                switch (param.BodyType)
                {
                    case RequestBodyType.StringContent:
                        string _serialized_content = null, mediatype = null;
                        switch (param.StringBodyFormat)
                        {
                            case StringContentFormat.Json:
                                _serialized_content = param.IsSerialized ? param.Value as string : param.ToJson(JsonConverters?.Values?.ToList());
                                mediatype = "application/json";
                                break;
                            case StringContentFormat.XML:
                                _serialized_content = param.IsSerialized ? param.Value as string : param.ToXml().ToString();
                                mediatype = "application/xml";
                                break;
                        }
                        result = new StringContent(_serialized_content, Encoding.UTF8, mediatype);
                        break;
                    case RequestBodyType.ByteArrayContent:
                    case RequestBodyType.StreamContent:
                        if (param.Value is byte[] byteContent)
                        {
                            //If byte content.
                            result = new ByteArrayContent(byteContent,0,byteContent.Length);
                        }
                        else if(param.Value is Stream streamContent)
                        {
                            //If stream content.
                            result = new StreamContent(streamContent);
                            result.Headers.Remove("Content-Type");
                            result.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            result.Headers.ContentDisposition = new ContentDispositionHeaderValue("stream-data") { FileName = param.FileName ?? "attachment" };
                        }
                        else
                        {
                            param.BodyType = RequestBodyType.StringContent;
                            return _createContent(param); //If the input is not byte array, then change it to string content and process again.
                        }
                        break;
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return null;
            }
        }
        private HttpContent _createContent(IEnumerable<RestParam> paramList, Method method)
        {
            //If body count is more than one, add as mulit form data. Else add as a single content of the specific body type.
            try
            {
                HttpContent result = null;
                if (method == Method.Get) return result; //Get cannot have a body.
                var _requestbodies = paramList.Where(p => p.ParamType == ParamType.RequestBody);

                if (_requestbodies == null || _requestbodies?.Count() == 0) return result;

                if (_requestbodies.Count() == 1)
                {
                    //If one item add as a direct body.
                    var target = _requestbodies.FirstOrDefault();
                    result = _createContent(target);
                }
                else
                {
                    //For more than one add as form data.
                    MultipartFormDataContent form_content = new MultipartFormDataContent();
                    form_content.Headers.Remove("Content-Type");
                    form_content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
                    foreach (var item in _requestbodies)
                    {
                        if (string.IsNullOrWhiteSpace(item.FileName))
                        {
                            form_content.Add(_createContent(item), item.Key); //Also add the key.
                        }
                        else
                        {
                            form_content.Add(_createContent(item), item.Key, item.FileName); //File name cannot be empty. Sending empty variable throws exception/
                        }
                    }
                    result = form_content;
                }

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private string _createQuery(string url, IEnumerable<RestParam> paramList)
        {
            string result = url;
            var _query = HttpUtility.ParseQueryString(string.Empty);

            //Assuming all the inputs are serialzied or direct values.
            foreach (var param in paramList.Where(p=>p.ParamType == ParamType.QueryString))
            {
                //We only process if the content is string.
                if (param.Value is string strValue)
                {
                    _query[param.Key] = strValue; //Doesn't care if it is serialized or not. We just take the string.
                }
            }
            var _formed_query = _query.ToString();
            if(!string.IsNullOrWhiteSpace(_formed_query))
            {
                result = result + "?" + _formed_query;
            }
            return result;
        }

        private (HttpContent content,string url) processInputs(string url, IEnumerable<RestParam> paramList, Method method)
        {
            try
            {
                //HTTPCONENT itself is a abstract class. We can have StringContent, StreamContent,FormURLEncodedContent,MultiPartFormdataContent.
                //Based on the params, we might add the data to content or to the url (in case of get).
                if (paramList == null || paramList?.Count() == 0) return (null,url);
                HttpContent processed_content = null;
                string processed_url = url;

                //A get request cannot have a content body.
                processed_content = _createContent(paramList, method);
                processed_url = _createQuery(url, paramList);
                return (processed_content, processed_url);
            }
            catch (Exception ex )
            {
                throw ex;
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
