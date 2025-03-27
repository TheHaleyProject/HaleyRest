using Haley.Abstractions;
using Haley.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Haley.Models {
    //GET METHODS WITH A BODY: https://stackoverflow.com/questions/978061/http-get-with-request-body

    /// <summary>
    /// A simple straightforward HTTPClient Wrapper.
    /// </summary>
    public sealed class FluentClient : RestBase, IClient {
        public HttpClient BaseClient { get; private set; }
        public string FriendlyName { get; set; }

        #region Attributes
        Func<HttpRequestMessage, Task<bool>> _request_validation_cb;
        HttpClientHandler _handler = new HttpClientHandler();
        Uri _base_uri;
        bool _auto_authenticate = true;
        #endregion

        #region Constructors
        public FluentClient(string base_address, string friendly_name, Func<HttpRequestMessage, Task<bool>> request_validationcallback, ILogger logger) : base(base_address) {
            _base_uri = CreateURI(base_address);
            _request_validation_cb = request_validationcallback;
            if (string.IsNullOrWhiteSpace(friendly_name)) friendly_name = base_address;
            FriendlyName = friendly_name;
            base.SetLogger(logger);
            BaseClient = new HttpClient(_handler, false); //Base client is read only. So initiate only once.
            if (_base_uri == null || string.IsNullOrWhiteSpace(base_address)) {
                WriteLog(LogLevel.Debug, $@"Warning: Base URI for the Client is null. Please add base uri to client or the rest request endpoints needs to be provided with full URL for proper execution");
            } else {
                BaseClient.BaseAddress = _base_uri; //Address can be set only once. Calling multiple times will throw exception.
            }
            ResetHeaders();
        }
        public FluentClient(string base_address, string friendly_name, ILogger logger) : this(base_address, friendly_name, null, logger) { }
        public FluentClient(string base_address, ILogger logger) : this(base_address, base_address, null, logger) { }
        public FluentClient(string base_address) : this(base_address, "Fluent Client", null) { }
        public FluentClient() : this(string.Empty, "Fluent Client", null) { }

        public FluentClient SetHandler(HttpClientHandler handler, bool disposehandler = true) {
            if (handler != null) {
                //our client might already have the base address setup in the constructor.
                //So fetch that first
                var targetBaseAddress = BaseClient.BaseAddress;
                var newclient = new HttpClient(handler, disposehandler);
                newclient.BaseAddress = targetBaseAddress;
                BaseClient = newclient;
            }
            return this;
        }
        #endregion

        public override string ToString() {
            return this.FriendlyName;
        }

        public IClient UpdateFriendlyName(string friendlyName) {
            FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? "Fluent Client" : friendlyName;
            return this;
        }

        private IRequest GetNewRequest() {
            var result = new RestRequest().SetClient(this);
            if (_auto_authenticate) {
                result.InheritAuthentication(_auto_authenticate);
            }
            return result;
        }

        public IClient AutoAuthenticateRequests(bool auto_authenticate = true) {
            _auto_authenticate = auto_authenticate;
            return this;
        }
        public IRequest CreateRequest() {
            return GetNewRequest();
        }
        public override IRequest WithEndPoint(string resource_url_endpoint) {
            return GetNewRequest().WithEndPoint(resource_url_endpoint);
            //Method chaining will ensure that the subsequent calls are made to the restrequest and not to this client as we are returning a rest request here..
        }
        public override IRequest AddCancellationToken(CancellationToken cancellation_token) {
            return GetNewRequest().AddCancellationToken(cancellation_token);
        }

        public override IRequest WithParameter(IRequestContent param) {
            return GetNewRequest().WithParameter(param);
        }

        public override IRequest WithBody(object content, bool is_serialized, BodyContentType content_type) {
            return GetNewRequest().WithBody(content, is_serialized, content_type);
        }
        public override IRequest WithBody(IRawBodyRequestContent rawBodyRequest) {
            return GetNewRequest().WithBody(rawBodyRequest);
        }
        public override IRequest WithParameters(IEnumerable<IRequestContent> parameters) {
            return GetNewRequest().WithParameters(parameters);
        }

        public override IRequest WithContent(HttpContent content) {
            return GetNewRequest().WithContent(content);
        }
        public override IRequest WithQuery(IQueryRequestContent param) {
            return GetNewRequest().WithQuery(param);
        }

        public override IRequest WithForm(IFormRequestContent param) {
            return GetNewRequest().WithForm(param);
        }

        public override IRequest WithQueries(IEnumerable<IQueryRequestContent> parameters) {
            return GetNewRequest().WithQueries(parameters);
        }

        public override IRequest WithProgressReporter(IProgressReporter reporter) {
            return GetNewRequest().WithProgressReporter(reporter);
        }

        public override async Task<RestResponse<T>> GetAsync<T>() {
            return await GetNewRequest().GetAsync<T>();
        }

        public override async Task<IResponse> GetAsync() {
            return await GetNewRequest().GetAsync();
        }

        public override async Task<IResponse> PostAsync() {
            return await GetNewRequest().PostAsync();
        }

        public override async Task<IResponse> PutAsync() {
            return await GetNewRequest().PutAsync();
        }

        public override async Task<IResponse> DeleteAsync() {
            return await GetNewRequest().DeleteAsync();
        }

        public override async Task<IResponse> SendAsync(Method method) {
            return await GetNewRequest().SendAsync(method);
        }

        public IClient WithRequestValidation(Func<HttpRequestMessage, Task<bool>> validationCallBack) {
            _request_validation_cb = validationCallBack;
            return this;
        }

        public IClient WithTimeOut(TimeSpan timeout) {
            BaseClient.Timeout = timeout;
            return this;
        }

        public Func<HttpRequestMessage, Task<bool>> GetRequestValidation() {
            return _request_validation_cb;
        }

        public async Task<IResponse> SendAsync(HttpRequestMessage request) {
            //IRequest has no SendAsync but RestRequest has the sendAsync

            return await ((new RestRequest().SetClient(this)) as RestRequest).SendAsync(request);
        }

        #region Common Methods
        public new IClient SetAuthenticator(IAuthProvider authenticator) {
            base.SetAuthenticator(authenticator);
            return this;
        }

        public new IClient RemoveAuthenticator() {
            base.RemoveAuthenticator();
            return this;
        }

        public new IClient SetAuthParam(IAuthParam auth_param) {
            base.SetAuthParam(auth_param);
            return this;
        }

        public new IClient ResetHeaders() {
            base.ResetHeaders();
            return this;
        }

        public new IClient ResetHeaders(Dictionary<string, IEnumerable<string>> reset_values) {
            base.ResetHeaders(reset_values);
            return this;
        }

        public new IClient AddDefaultHeaders() {
            base.AddDefaultHeaders();
            return this;
        }

        public new IClient AddHeader(string name, string value) {
            base.AddHeader(name, value);
            return this;
        }

        public new IClient AddHeaderValues(string name, List<string> values) {
            base.AddHeaderValues(name, values);
            return this;
        }

        public new IClient RemoveHeader(string name) {
            base.RemoveHeader(name);
            return this;
        }

        public new IClient ReplaceHeader(string name, string value) {
            base.ReplaceHeader(name, value);
            return this;
        }

        public new IClient ReplaceHeaderValues(string name, List<string> values) {
            base.ReplaceHeaderValues(name, values);
            return this;
        }

        public new IClient AddJsonConverter(JsonConverter converter) {
            base.AddJsonConverter(converter);
            return this;
        }

        public new IClient RemoveJsonConverter(JsonConverter converter) {
            base.RemoveJsonConverter(converter);
            return this;
        }

        public new IClient SetLogger(ILogger logger) {
            base.SetLogger(logger);
            return this;
        }

        #endregion

    }
}
