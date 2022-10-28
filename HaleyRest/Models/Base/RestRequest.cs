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
using static System.Net.Mime.MediaTypeNames;

namespace Haley.Models
{
    //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body

    /// <summary>
    /// A simple straightforward HTTPClient Wrapper.
    /// </summary>
    public sealed class RestRequest : RestBase, IRequest
    {
        HttpRequestMessage _request = null;  //Prio-1
        HttpContent _content = null; //Prio-2
        IEnumerable<RequestObject> _requestObjects = new List<RequestObject>();//Prio-3
        bool _inherit_headers = false;
        bool _inherit_authentication = false;
        bool _inherit_auth_param = false;
        IProgressReporter _reporter = null;
        #region Attributes
        string _boundary = "----CustomBoundary" + DateTime.Now.Ticks.ToString("x");
        CancellationToken? _cancellation_token = null;
        public IClient Client { get; private set; }
        #endregion

        #region Constructors
        public RestRequest(string end_point_url, IClient client) : base(end_point_url) {
            Client = client;
        }
        public RestRequest(string end_point_url) : this(end_point_url,null) { }
        public RestRequest() : this(string.Empty, null) { }
        #endregion

        #region Request Creation
        public override IRequest WithQuery(QueryParam param) {
            return WithParameter(param);
        }
        public override IRequest WithQueries(IEnumerable<QueryParam> parameters) {
            return WithParameters(parameters);
        }
        public override IRequest WithBody(object content, bool is_serialized, BodyContentType content_type) {
            return WithParameter(new RawBodyRequest(content, is_serialized, content_type));
        }
        public override IRequest WithParameter(RequestObject param) {
            return WithParameters(new List<RequestObject>() { param });
        }
        public override IRequest WithParameters(IEnumerable<RequestObject> parameters) {
            _requestObjects = parameters;
            return this;
        }
        public override IRequest WithContent(HttpContent content) {
            _content = content;
            return this;
        }
        #endregion

        #region Base Fluent Methods
        public IRequest SetClient(IClient client) {
            this.Client = client;
            return this;
        }
        public override IRequest WithEndPoint(string resource_url_endpoint) {
            URL = ParseURI(resource_url_endpoint).pathQuery; //What if we needed to use full URL?
            return this;
        }

        public override IRequest AddCancellationToken(CancellationToken cancellation_token) {
            this._cancellation_token = cancellation_token;
            return this;
        }

        public override IRequest WithProgressReporter(IProgressReporter reporter) {
            _reporter = reporter;
            return this;
        }

        #endregion

        #region Get Methods
        public override async Task<RestResponse<T>> GetAsync<T>() {
            var _response = await GetAsync();
            var _options = GetSerializerOptions();
            var result = await new RestResponse<T>(_response.OriginalResponse)
                               .SetConveter((str) => { return JsonSerializer.Deserialize<T>(str,_options); })
                               .FetchContent();
            return result;
        }
        public override async Task<IResponse> GetAsync() {
            return await SendAsync(Method.GET);
        }
        public override async Task<IResponse> PostAsync() {
            return await SendAsync(Method.POST);
        }
        public override async Task<IResponse> PutAsync() {
            return await SendAsync(Method.PUT);
        }
        public override async Task<IResponse> DeleteAsync() {
            return await SendAsync(Method.DELETE);
        }
        public override async Task<IResponse> SendAsync(Method method) {
            ValidateClient();
            if (URL == null) URL = string.Empty;
            if (_request != null) {
                //Prio 1 : If request is available.
                return await SendAsync(_request);
            } else if(_content != null) {
                //Prio 2 : If content is availble without request.
                return await SendAsync(_content,URL, method); //Send associated URL without parsing
            } else if(_requestObjects != null && _requestObjects.Count() > 0) {
                //Prio 3: Conver the request objects to httpcontent.
                var processedInputs = ConverToHttpContent(URL, _requestObjects, method); //Here, URL is just the end point.
                return await SendAsync(processedInputs.content, processedInputs.url, method);
            }
            else {
                //No content, no queries. Just send the plain request with the given method.
                return await SendAsync(null,URL, method);
            }
        }

        #endregion

        #region Send Methods

        private string GetAuthValue(IRestBase source,HttpRequestMessage request) {
            var authenticator = FetchAuthenticator(this);
            var authparam = FetchAuthParam(this);

            //When calling the authenticator from internally, let's also add url_decode because we know that we are already encoding (Uri.EscapeDataString) the query parameters (both Key and value).
            //"true" after authparam is for url_decode
            return authenticator?.GenerateToken(this.Client?.BaseClient?.BaseAddress, request, authparam,true) ?? String.Empty;
        }

        private IAuthProvider FetchAuthenticator(IRestBase source) {
            var authenticator = source.GetAuthenticator();
            if (authenticator != null) {
                return authenticator;
            } else if (source is RestRequest res_req && res_req._inherit_authentication && res_req.Client != null) {
                return FetchAuthenticator(res_req.Client);
            }
            return null;
        }

        private object FetchAuthParam(IRestBase source) {
            var param = source.GetAuthParam();
            if (param != null) {
                return param;
            } else if (source is RestRequest res_req && res_req._inherit_auth_param && res_req.Client != null) {
                return FetchAuthParam(res_req.Client);
            }
            return null;
        }

        async Task<IResponse> SendAsync(HttpContent content, string url, Method method) {

            WriteLog(LogLevel.Information, $@"Initiating a {method} request to {url} with base url {Client.URL}");
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
            var uri_components = ParseURI(url);
            var resource_Url = uri_components.pathQuery;

            if (string.IsNullOrWhiteSpace(Client.URL)) { //if the client url is empty, then we conside the request url
                resource_Url = URL; //Take the full url, irrespective of whatever is provided, assuming that the URL is absolute.
            }

            //SET REQUEST PROPERTY VALUE
            _request = new HttpRequestMessage(request_method, resource_Url);

            if (content != null) {
                _request.Content = content; //Set content if not null
            }

            #region Authentication and Headers
            var _headers = GetHeaders();
            if (_inherit_headers) {
                var _parentHeaders = Client?.GetHeaders();
                //Update values from here onto the _headers

            }

            if (_headers?.Count > 0) {
                foreach (var kvp in _headers) {
                    try {
                        _request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value); //Do not validate.
                    }
                    catch (Exception ex) {
                        WriteLog(LogLevel.Debug, new EventId(2001, "Header Error"), "Error while trying to add a header", ex);
                    }
                }
            }
            #endregion

            var result = await SendAsync(method);
            return result; //All calls from here will receive stringResponse content.
        }
        internal async Task<IResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellation_token) {
            this._cancellation_token = cancellation_token;
            return await SendAsync(request);
        }

        internal async Task<IResponse> SendAsync(HttpRequestMessage request) {
            this._request = request;
            ValidateClient();
            //Authorization should happen only here because we would only add all query params before this stage.
            if (_request.Headers.Contains(RestConstants.Headers.Authorization)) {
                _request.Headers.Remove(RestConstants.Headers.Authorization);
            }

            //_request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", GetAuthValue(this, _request)); //if the input is not correct, for instance, 
            _request.Headers.TryAddWithoutValidation(RestConstants.Headers.Authorization, GetAuthValue(this, _request));

            var _validationCB = Client.GetRequestValidation();

            //if some sort of validation callback is assigned, then call that first.
            if (_validationCB != null) {
                var validation_check = await _validationCB.Invoke(request);
                if (!validation_check) {
                    WriteLog(LogLevel.Information, "Local request validation failed. Please verify the validation methods to return true on successful validation");
                    return new BaseResponse(null).SetMessage("Internal Request Validation call back failed.");
                }
            }

            //Here we donot modify anything. We just send and receive the response.
            HttpResponseMessage message;
            if (_cancellation_token != null) {
                message = await Client.BaseClient.SendAsync(request, _cancellation_token.Value);
            }
            else {
                message = await Client.BaseClient.SendAsync(request);
            }

            return new BaseResponse(message);
        }
        
        #endregion

        #region Helpers

        private void ValidateClient() {
            if (Client == null) throw new ArgumentNullException(nameof(Client));
        }
        (HttpContent content, string url) ConverToHttpContent(string url, IEnumerable<RequestObject> paramList, Method method) {
            //HTTPCONENT itself is a abstract class. We can have StringContent, StreamContent,FormURLEncodedContent,MultiPartFormdataContent.
            //Based on the params, we might add the data to content or to the url (in case of get).
            if (paramList == null || paramList?.Count() == 0) return (null, url ?? string.Empty);
            HttpContent processed_content = null;
            string processed_url = url;

            //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body
            //A get request can have a content body.

            //The paramlist might containt multiple request param(which will be trasformed in to query). however, only one (the first) request body will be considered
            processed_content = PrepareBody(paramList, method);
            processed_url = PrepareQuery(url, paramList);
            return (processed_content, processed_url);
        }
        HttpContent PrepareBody(IEnumerable<RequestObject> paramList, Method method) {
            //We can add only one type of body to an object. If we have more than one type, we log the error and take only the first item.
            try {
                HttpContent result = null;
                //paramList.Where(p=> typeof(IRequestBody).IsAssignableFrom(p))?.f
                var _requestBody = paramList.Where(p => p is IRequestBody)?.FirstOrDefault();
                if (_requestBody == null || _requestBody.Value == null) return result; //Not need of further processing for null values.
                WriteLog(LogLevel.Debug, $@"Request body of type {_requestBody?.GetType()} is getting added to request body.");
                if (_requestBody is RawBodyRequest rawReq) {
                    //Just add a raw content and send.
                    result = PrepareRawBody(rawReq);

                }
                else if (_requestBody is FormBodyRequest formreq) {
                    //Decide if this is multipart form or urlencoded form data
                    result = PrepareFormBody(formreq);
                }
                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(6000), "Error while trying to prepare body", ex);
                return null;
            }
        }
        string PrepareQuery(string url, IEnumerable<RequestObject> paramList) {
            string result = url;
            var _query = HttpUtility.ParseQueryString(string.Empty);

            var _paramQueries = paramList.Where(p => p is IRequestQuery)?.Cast<IRequestQuery>().ToList();
            if (_paramQueries == null || _paramQueries.Count == 0) return result; //return the input url

            foreach (var param in _paramQueries) {
                var _key = param.Key;
                var _value = param.Value;

                if (param.CanEncode) {
                    //Encode before adding
                    if (param.CanEncode) {
                        _key = Uri.EscapeDataString(_key);
                        _value = Uri.EscapeDataString(_value);
                        param.SetEncoded(); //cannot encode again, its already encoded
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
        HttpContent PrepareRawBody(RawBodyRequest rawbody) {
            try {

                //Dont use the Decription of the rawbody request anywhere. It will be used only for internal purpose

                HttpContent result = null;
                string mediatype = string.IsNullOrWhiteSpace(rawbody.MIMEType)? "application/octet-stream" : rawbody.MIMEType;

                switch (rawbody.BodyType) {
                    case BodyContentType.StringContent:
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

                        if (_reporter != null) {
                            byte[] byteArray = Encoding.UTF8.GetBytes(_serialized_content);
                            MemoryStream stream = new MemoryStream(byteArray);
                            result = new ProgressableStreamContent(stream, _reporter, rawbody.Id) { Title = rawbody.Title, Description = rawbody.Description};
                            result.Headers.ContentDisposition = new ContentDispositionHeaderValue("stream-data") { FileName = rawbody.Title ?? "attachment" };
                        } else {
                            //string content.
                            result = new StringContent(_serialized_content, Encoding.UTF8);
                        }
                        break;

                    case BodyContentType.ByteArrayContent:
                    case BodyContentType.StreamContent:
                        if (rawbody.Value is byte[] byteContent) {
                            //If byte content.
                            result = new ByteArrayContent(byteContent, 0, byteContent.Length);
                        }
                        else if (rawbody.Value is Stream streamContent) {
                            if (_reporter != null) {
                                result = new ProgressableStreamContent(streamContent,_reporter,rawbody.Id);
                            } else {
                                //If stream content.
                                result = new StreamContent(streamContent);
                            }
                            //Dont' remove all headers. Only the content type. Header might have authentications properly set.
                            result.Headers.ContentDisposition = new ContentDispositionHeaderValue("stream-data") { FileName = rawbody.Title ?? "attachment" };
                        }
                        break;
                }

                result.Headers.Remove("Content-Type");
                result.Headers.ContentType = new MediaTypeHeaderValue(mediatype);
                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(6001), "Error while trying to prepare Raw body", ex);
                return null;
            }
        }
        HttpContent PrepareFormBody(FormBodyRequest formbody) {
            try {
                HttpContent result = null;
                //Form can be url encoded form and multi form.. //TODO : REFINE
                //For more than one add as form data.
                MultipartFormDataContent form_content = new MultipartFormDataContent();
                form_content.Headers.Remove("Content-Type");
                form_content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + _boundary);

                foreach (var item in formbody.Value) {
                    if (item.Value == null) continue;
                    var rawContent = PrepareRawBody(item.Value);
                    if (string.IsNullOrWhiteSpace(item.Value.Title)) {
                        form_content.Add(rawContent, item.Key); //Also add the key.
                    }
                    else {
                        form_content.Add(rawContent, item.Key, item.Value.Title); //File name cannot be empty. Sending empty variable throws exception/
                    }
                }

                return result;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Trace, new EventId(1003), "Error while trying to prepare Form body", ex);
                return null;
            }
        }
        #endregion

        #region Common Methods
        public new IRequest SetAuthenticator(IAuthProvider authenticator) {
            base.SetAuthenticator(authenticator);
            return this;
        }

        public new IRequest RemoveAuthenticator() {
            base.RemoveAuthenticator();
            return this;
        }

        public new IRequest SetAuthParam(object auth_param) {
            base.SetAuthParam(auth_param);
            return this;
        }

        public new IRequest ResetHeaders() {
            base.ResetHeaders();
            return this;
        }

        public new IRequest ResetHeaders(Dictionary<string, IEnumerable<string>> reset_values) {
            base.ResetHeaders(reset_values);
            return this;
        }

        public new IRequest AddDefaultHeaders() {
            base.AddDefaultHeaders();
            return this;
        }

        public new IRequest AddHeader(string name, string value) {
            base.AddHeader(name, value);
            return this;
        }

        public new IRequest RemoveHeader(string name) {
            base.RemoveHeader(name);
            return this;
        }

        public new IRequest AddHeaderValues(string name, List<string> values) {
            base.AddHeaderValues(name, values);
            return this;
        }
        public new IRequest ReplaceHeader(string name, string value) {
            base.ReplaceHeader(name, value);
            return this;
        }

        public new IRequest ReplaceHeaderValues(string name, List<string> values) {
            base.ReplaceHeaderValues(name, values);
            return this;

        }
        public new IRequest AddJsonConverter(JsonConverter converter) {
            base.AddJsonConverter(converter);
            return this;
        }

        public new IRequest RemoveJsonConverter(JsonConverter converter) {
            base.RemoveJsonConverter(converter);
            return this;
        }

        public new IRequest SetLogger(ILogger logger) {
            base.SetLogger(logger);
            return this;
        }

        public IRequest InheritHeaders(bool inherit) {
            _inherit_headers = inherit;
            return this;
        }

        public IRequest InheritAuthentication(bool inherit_authenticator, bool inherit_parameter) {
            _inherit_authentication = inherit_authenticator;
            _inherit_auth_param = inherit_parameter;
            return this;
        }

        #endregion
        public override string ToString()
        {
            return this.URL;
        }
    }
}
