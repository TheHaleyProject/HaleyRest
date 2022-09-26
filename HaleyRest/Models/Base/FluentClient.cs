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
    public sealed class FluentClient :RestBase, IClient
    {
        public HttpClient BaseClient { get;  }
        public string FriendlyName { get; private set; }

        #region Attributes
        Func<HttpRequestMessage, Task<bool>> _request_validation_cb;
        HttpClientHandler _handler = new HttpClientHandler();
        Uri _base_uri;
        #endregion

        #region Constructors
        public FluentClient(string base_address,string friendly_name ,Func<HttpRequestMessage, Task<bool>> request_validationcallback,ILogger logger):base(base_address)
        {
            _base_uri = CreateURI(base_address);
            _request_validation_cb = request_validationcallback;
            if (string.IsNullOrWhiteSpace(friendly_name)) friendly_name = base_address;
            FriendlyName = friendly_name;
            base.SetLogger(logger);
            BaseClient = new HttpClient(_handler, false); //Base client is read only. So initiate only once.
            if (_base_uri == null || string.IsNullOrWhiteSpace(base_address)) {
                WriteLog(LogLevel.Debug, $@"Warning: Base URI for the Client is null. Please add base uri to client or the rest request endpoints needs to be provided with full URL for proper execution");
            }
            else {
                BaseClient.BaseAddress = _base_uri; //Address can be set only once. Calling multiple times will throw exception.
            }
            ResetHeaders();
        }
        public FluentClient(string base_address, string friendly_name,ILogger logger) : this(base_address, friendly_name,null,logger) { }
        public FluentClient(string base_address, ILogger logger) : this(base_address, base_address, null,logger) { }
        public FluentClient(ILogger logger) : this(string.Empty, "Fluent Client", logger) { }
        public FluentClient() : this(string.Empty, "Fluent Client", null) { }

        #endregion

        public override string ToString()
        {
            return this.FriendlyName;
        }

        public IClient UpdateFriendlyName(string friendlyName) {
            FriendlyName = string.IsNullOrWhiteSpace(friendlyName)? "Fluent Client":friendlyName;
            return this;
        }

        public override IRestBase WithEndPoint(string resource_url_endpoint) {
            return new RestRequest().WithEndPoint(resource_url_endpoint);
            //Method chaining will ensure that the subsequent calls are made to the restrequest and not to this client as we are returning a rest request here..
        }

        public override IRestBase AddCancellationToken(CancellationToken cancellation_token) {
            return new RestRequest().AddCancellationToken(cancellation_token);
        }
       
        public override IRestBase WithParameter(RequestObject param) {
            return new RestRequest().WithParameter(param);
        }

        public override IRestBase WithBody(object content, bool is_serialized, BodyContentType content_type) {
            return new RestRequest().WithBody(content, is_serialized, content_type);
        }

        public override IRestBase WithParameters(IEnumerable<RequestObject> parameters) {
            return new RestRequest().WithParameters(parameters);
        }

        public override IRestBase WithContent(HttpContent content) {
            return new RestRequest().WithContent(content);
        }
        public override IRestBase WithQuery(QueryParam param) {
            return new RestRequest().WithQuery(param);
        }

        public override IRestBase WithQueries(IEnumerable<QueryParam> parameters) {
            return new RestRequest().WithQueries(parameters);
        }

        public override async Task<RestResponse<T>> GetAsync<T>() {
            return await new RestRequest().GetAsync<T>();
        }

        public override async Task<RestResponse> GetAsync() {
            return await new RestRequest().GetAsync();
        }

        public override async Task<IResponse> PostAsync() {
            return await new RestRequest().PostAsync();
        }

        public override async Task<IResponse> PutAsync() {
            return await new RestRequest().PutAsync();
        }

        public override async Task<IResponse> DeleteAsync() {
            return await new RestRequest().DeleteAsync();
        }

        public override async Task<IResponse> SendAsync(Method method) {
            return await new RestRequest().SendAsync(method);
        }

        public IClient WithRequestValidation(Func<HttpRequestMessage, Task<bool>> validationCallBack) {
            _request_validation_cb = validationCallBack;
            return this;
        }

        public Func<HttpRequestMessage, Task<bool>> GetRequestValidation() {
            return _request_validation_cb;
        }

        public async Task<IResponse> ExecuteAsync(HttpRequestMessage request) {
            return await new RestRequest().ExecuteAsync(request);
        }
    }
}
