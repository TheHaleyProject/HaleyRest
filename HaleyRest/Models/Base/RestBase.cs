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
    //Error IDs begin with 1000's
    /// <summary>
    /// A simple straightforward HTTPClient Wrapper.
    /// </summary>
    public abstract class RestBase : IRestBase
    {
        public string Id { get; }
        public string URL { get; protected set; }

        #region Attributes
        ILogger _logger;
        IAuthenticator _authenticator;
        ConcurrentDictionary<string, IEnumerable<string>> _headers = new ConcurrentDictionary<string, IEnumerable<string>>();
        ConcurrentDictionary<Type, JsonConverter> _jsonConverters = new ConcurrentDictionary<Type, JsonConverter>();
        #endregion

        #region Constructors
        public RestBase(string url)
        {
            Id = Guid.NewGuid().ToString();
            URL = url;
        }
        #endregion

        #region Interface Methods
        public IRestBase SetLogger(ILogger logger) {
            this._logger = logger;
            return this;
        }
        public IRestBase AddJsonConverter(JsonConverter converter) {
            try {
                if (converter == null) return this;

                if (!_jsonConverters.ContainsKey(converter.GetType())) {
                    _jsonConverters.TryAdd(converter.GetType(), converter);
                }
                return this;
            }
            catch (Exception ex) {
                EventId _eventid = new EventId(1001, "JSONConverter Add Error");
                WriteLog(LogLevel.Trace, _eventid, "Error while trying to JSON Converter", ex);
                return this;
            }
        }
        public IRestBase RemoveJsonConverter(JsonConverter converter) {
            try {
                if (converter == null) return this;
                var _type = converter.GetType();
                if (_jsonConverters.ContainsKey(_type)) {
                    _jsonConverters.TryRemove(_type, out var removed);
                }
                return this;
            }
            catch (Exception ex) {
                EventId _eventid = new EventId(1002, "JSONConverter Remove Error");
                WriteLog(LogLevel.Trace, _eventid, "Error while trying to JSON Converter", ex);
                return this;
            }
        }
        public virtual IRestBase ResetHeaders() {
            WriteLog(LogLevel.Debug, "Resetting the client headers");
            _headers.Clear();
            //Add Default headers

            //remains the same throught the life time of this client.
            //BaseClient.BaseAddress = _base_uri; //Base address cannot be reset multiple times.
            //BaseClient.DefaultRequestHeaders.Accept.Clear();
            //BaseClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));
            //BaseClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
            return this;
        }

        public IRestBase ResetHeaders(Dictionary<string, IEnumerable<string>> reset_values) {
            if (reset_values == null || reset_values.Count == 0) {
                _headers = new ConcurrentDictionary<string, IEnumerable<string>>();
            }
            else {
                _headers = new ConcurrentDictionary<string, IEnumerable<string>>(reset_values);
            }
            return this;
        }
        public IRestBase ClearAllHeaders() {
            _headers.Clear();
            return this;
        }
        public IRestBase AddHeaders(string name, string value) {
            _headers?.TryAdd(name, new List<string>() { value });
            return this;
        }
        public IRestBase AddHeaders(string name, List<string> values) {
            _headers?.TryAdd(name, values);
            return this;
        }
        public Dictionary<string, IEnumerable<string>> GetHeaders() {
            return _headers.ToDictionary(p => p.Key, q => q.Value);
        }
        public IRestBase ClearAuthentication() {
            _authenticator = null;
            return this;
        }
        public IRestBase SetAuthenticator(IAuthenticator authenticator) {
            _authenticator = authenticator;
            return this;
        }
        #endregion

        #region Helpers
        protected (HttpContent content, string url) ConverToHttpContent(string url, IEnumerable<RequestObject> paramList, Method method) {
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
            }
            catch (Exception ex) {
                throw ex;
            }
        }
        protected string LogFormatter(string state, Exception exception) {
            if (exception == null) return state;
            return (state + Environment.NewLine + exception.Message.ToString() + Environment.NewLine + exception.StackTrace.ToString());
        }

        protected void WriteLog(LogLevel level, EventId evtId, string msg, Exception ex) {
            _logger?.Log(level, evtId, msg, ex, LogFormatter);
        }

        protected void WriteLog(LogLevel level, string msg) {
            _logger?.Log(level, msg);
        }
        protected string PrepareToken(string token, string token_prefix) {
            try {
                var _token = string.Concat(token_prefix ?? "", " ", token);
                return _token?.Trim();
            }
            catch (Exception) {
                return null;
            }
        }
        protected HttpContent PrepareBody(IEnumerable<RequestObject> paramList, Method method) {
            //We can add only one type of body to an object. If we have more than one type, we log the error and take only the first item.
            try {
                HttpContent result = null;
                //paramList.Where(p=> typeof(IRequestBody).IsAssignableFrom(p))?.f
                var _requestBody = paramList.Where(p => p is IRequestBody)?.FirstOrDefault();
                if (_requestBody == null || _requestBody.Value == null) return result; //Not need of further processing for null values.

                if (_requestBody is RawBodyRequest rawReq) {
                    //Just add a raw content and send.
                    result = prepareRawBody(rawReq);

                }
                else if (_requestBody is FormBodyRequest formreq) {
                    //Decide if this is multipart form or urlencoded form data
                    result = prepareFormBody(formreq);
                }
                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(6000), "Error while trying to prepare body", ex);
                return null;
            }
        }
        protected string PrepareQuery(string url, IEnumerable<RequestObject> paramList) {
            string result = url;
            var _query = HttpUtility.ParseQueryString(string.Empty);

            var _paramQueries = paramList.Where(p => p is IRequestQuery)?.Cast<IRequestQuery>().ToList();
            if (_paramQueries == null || _paramQueries.Count == 0) return result; //return the input url

            foreach (var param in _paramQueries) {
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
            if (!string.IsNullOrWhiteSpace(_formed_query)) {
                result = result + "?" + _formed_query;
            }
            return result;
        }
        protected HttpContent prepareRawBody(RawBodyRequest rawbody) {
            try {
                HttpContent result = null;
                switch (rawbody.BodyType) {
                    case BodyContentType.StringContent:
                        string mediatype = null;
                        string _serialized_content = rawbody.Value as string; //Assuming it is already serialized.

                        switch (rawbody.StringBodyFormat) {
                            case StringContentFormat.Json:
                                if (!rawbody.IsSerialized) {
                                    _serialized_content = rawbody.ToJson(_jsonConverters?.Values?.ToList());
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
                                    _serialized_content = rawbody.ToJson(_jsonConverters?.Values?.ToList());
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
                        }
                        else if (rawbody.Value is Stream streamContent) {
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
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(6001), "Error while trying to prepare Raw body", ex);
                return null;
            }
        }
        protected HttpContent prepareFormBody(FormBodyRequest formbody) {
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
                    }
                    else {
                        form_content.Add(rawContent, item.Key, item.Value.FileName); //File name cannot be empty. Sending empty variable throws exception/
                    }
                }

                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(1003), "Error while trying to prepare Form body", ex);
                return null;
            }
        }
        protected (string authority, string pathQuery) parseURI(string input_url) {
            try {
                if (string.IsNullOrEmpty(input_url)) return (string.Empty, string.Empty);
                if (Uri.TryCreate(input_url, UriKind.RelativeOrAbsolute, out Uri _uri)) {
                    if (_uri.IsAbsoluteUri) {
                        string _authority = _uri.GetLeftPart(UriPartial.Authority);
                        string _method = input_url.Substring(_authority.Length);
                        return (_authority, _method);
                    }
                }
                return (string.Empty, input_url);
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(1004), "Error while trying to parse URI", ex);
                return (string.Empty, input_url);
            }
        }
        protected (string authority, string pathQuery) parseURI(Uri input_uri) {
            return parseURI(input_uri.AbsoluteUri);
        }
        protected Uri getBaseUri(string address) {
            bool result = Uri.TryCreate(address, UriKind.Absolute, out var uri_result)
                && (uri_result.Scheme == Uri.UriSchemeHttp || uri_result.Scheme == Uri.UriSchemeHttps);
            if (result) return uri_result;
            Console.WriteLine($@"ERROR: Unable to create URI from the address {address}");
            return null;
        }
        protected Uri getBaseUri(Uri inputURI) {
            return getBaseUri(inputURI.AbsoluteUri);
        }
        #endregion

        #region Abstract Methods
        public abstract IRestBase WithEndPoint(string resource_url_endpoint);
        public abstract IRestBase AddCancellationToken(CancellationToken cancellation_token);
        public abstract IRestBase CreateRequest();
        public abstract IRestBase CreateRequest(RequestObject param);
        public abstract IRestBase CreateRequest(object content, bool is_serialized, BodyContentType content_type);
        public abstract IRestBase CreateRequestWithParams(IEnumerable<RequestObject> parameters);
        public abstract IRestBase CreateRequestWithContent(HttpContent content);
        public abstract Task<SerializedResponse<T>> GetAsync<T>() where T : class;
        public abstract Task<StringResponse> GetAsync();
        public abstract Task<IResponse> PostAsync();
        public abstract Task<IResponse> PutAsync();
        public abstract Task<IResponse> DeleteAsync();
        public abstract Task<IResponse> SendAsync(Method method);
        #endregion
    }
}
