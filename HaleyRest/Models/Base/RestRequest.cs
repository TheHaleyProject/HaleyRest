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
    public sealed class RestRequest : RestBase, IRequest
    {
        HttpRequestMessage _request = null;  //Prio-1
        HttpContent _content = null; //Prio-2
        IEnumerable<RequestObject> _requestObjects = new List<RequestObject>();//Prio-3

        #region Attributes
        HttpClientHandler handler = new HttpClientHandler();
        static string boundary = "----CustomBoundary" + DateTime.Now.Ticks.ToString("x");
        bool _add_cancellation_token = false;
        CancellationToken _cancellation_token = default(CancellationToken);
        bool _inherit_headers = false;
        bool _inherit_authentication = false;
       
        public IClient Client { get; private set; }
        #endregion

        #region Constructors
        public RestRequest(string end_point_url, IClient client) : base(end_point_url) {
            Client = client;
        }
        public RestRequest(string end_point_url) : this(end_point_url,null) { }
        public RestRequest() : this(string.Empty, null) { }
        #endregion
        #region Base Fluent Methods
        public IRequest SetClient(IClient client) {
            this.Client = client;
            return this;
        }

        public IRequest InheritHeaders() {
            _inherit_headers = true;
            return this;
        }

        public IRequest InheritAuthentication() {
            _inherit_authentication = true;
            return this;
        }

        public override IRestBase WithEndPoint(string resource_url_endpoint) {
            URL = parseURI(resource_url_endpoint).pathQuery;
            return this;
        }

        public override IRestBase AddCancellationToken(CancellationToken cancellation_token) {
            this._cancellation_token = cancellation_token;
            _add_cancellation_token = true; //We have added a token, so we set it.
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
        #region region Request Creation
        public override IRestBase CreateRequest() {
            return CreateRequest(null);
        }
        public override IRestBase CreateRequest(object content, bool is_serialized, BodyContentType content_type) {
            return CreateRequest(new RawBodyRequest(content,is_serialized,content_type));
        }
        public override IRestBase CreateRequest(RequestObject param) {
            return CreateRequestWithParams(new List<RequestObject>() { param });
        }
        public override IRestBase CreateRequestWithParams(IEnumerable<RequestObject> parameters) {
            _requestObjects = parameters;
            return this;
        }
        public override IRestBase CreateRequestWithContent(HttpContent content) {
            _content = content;
            return this;
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
        public override Task<SerializedResponse<T>> GetAsync<T>() {
            throw new NotImplementedException();
        }
        public override Task<StringResponse> GetAsync() {
            throw new NotImplementedException();
        }
        public override Task<IResponse> PostAsync() {
            throw new NotImplementedException();
        }
        public override Task<IResponse> PutAsync() {
            throw new NotImplementedException();
        }
        public override Task<IResponse> DeleteAsync() {
            throw new NotImplementedException();
        }
        public override Task<IResponse> SendAsync(Method method) {
            throw new NotImplementedException();
        }
        internal async Task<IResponse> ExecuteAsync(HttpRequestMessage request) {
            this._request = request;
            //if some sort of validation callback is assigned, then call that first.
            if (_validationCallback != null) {
                var validation_check = await _validationCallback.Invoke(request);
                if (!validation_check) {
                    WriteLog(LogLevel.Information, "Local request validation failed. Please verify the validation methods to return true on successful validation");
                    return new StringResponse() { StringContent = "Internal Request Validation call back failed." };
                }
            }

            //Here we donot modify anything. We just send and receive the response.
            HttpResponseMessage message;
            if (_add_cancellation_token) {
                message = await Client.BaseClient.SendAsync(request, _cancellation_token);
            }
            else {
                message = await Client.BaseClient.SendAsync(request);
            }

            var _response = new BaseResponse() { OriginalResponse = message };
            return _response;
        }
        internal async Task<IResponse> ExecuteAsync(HttpRequestMessage request,CancellationToken cancellation_token) {
            this._request = request;
            //if some sort of validation callback is assigned, then call that first.
            if (_validationCallback != null) {
                var validation_check = await _validationCallback.Invoke(request);
                if (!validation_check) {
                    WriteLog(LogLevel.Information, "Local request validation failed. Please verify the validation methods to return true on successful validation");
                    return new StringResponse() { StringContent = "Internal Request Validation call back failed." };
                }
            }

            //Here we donot modify anything. We just send and receive the response.
            HttpResponseMessage message;
            if (_add_cancellation_token) {
                message = await Client.BaseClient.SendAsync(request, _cancellation_token);
            }
            else {
                message = await Client.BaseClient.SendAsync(request);
            }

            var _response = new BaseResponse() { OriginalResponse = message };
            return _response;
        }
        #endregion
        public override string ToString()
        {
            return this.URL;
        }
    }
}
