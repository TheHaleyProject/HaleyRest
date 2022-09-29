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
    /// Rest Base for both FluentClient and also RestRequest
    /// </summary>
    public abstract class RestBase : IRestBase
    {
        public string Id { get; }
        public string URL { get; protected set; }
        
        protected ConcurrentDictionary<Type, JsonConverter> _jsonConverters = new ConcurrentDictionary<Type, JsonConverter>();
        protected bool _inherit_headers = false;
        protected bool _inherit_authentication = false;
        protected bool _inherit_auth_param = false; 
        #region Attributes
        ILogger _logger;
        ConcurrentDictionary<string, IEnumerable<string>> _headers = new ConcurrentDictionary<string, IEnumerable<string>>();
        IAuthenticator _authenticator;
        object _authParam = null;
        #endregion

        #region Constructors
        protected RestBase(string url)
        {
            Id = Guid.NewGuid().ToString();
            URL = url;
            //On creation add default request headers.
            ResetHeaders();
            AddDefaultHeaders();
        }
        protected RestBase():this(string.Empty) { }
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
            WriteLog(LogLevel.Debug, "Clear all headers");
            _headers?.Clear();
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
        public IRestBase AddDefaultHeaders() {
            if (_headers == null) _headers = new ConcurrentDictionary<string, IEnumerable<string>>();
            //Add default values.
            AddHeader(RestConstants.Headers.Accept, "*/*");
            AddHeader(RestConstants.Headers.UserAgent, "HaleyFluentClient");
            AddHeaders(RestConstants.Headers.AcceptEncoding, new List<string>() { "gzip", "deflate", "br" });
            AddHeader(RestConstants.Headers.Connection, "keep-alive");
            AddHeader(RestConstants.Headers.CacheControl, "no-cache");
            return this;
        }
        public IRestBase AddHeader(string name, string value) {
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
        public IAuthenticator GetAuthenticator() {
            return _authenticator;

        }
        public object GetAuthParam() {
            return _authParam;
        }
        public IRestBase RemoveAuthenticator() {
            _authenticator = null;
            return this;
        }
        public IRestBase SetAuthenticator(IAuthenticator authenticator) {
            _authenticator = authenticator;
            return this;
        }
        public IRestBase InheritHeaders(bool inherit = true) {
            _inherit_headers = inherit;
            return this;
        }

        public IRestBase InheritAuthentication(bool inherit_authenticator = true, bool inherit_parameter = true) {
            _inherit_authentication = inherit_authenticator;
            _inherit_auth_param = inherit_parameter;
            return this;
        }

        public IRestBase SetAuthParam(object auth_param) {
            _authParam = auth_param;
            return this;
        }
        #endregion

        #region Helpers
        protected JsonSerializerOptions GetSerializerOptions() {
            var options =  new JsonSerializerOptions()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            };
            try {
                _jsonConverters.Values.ToList().ForEach(p => options.Converters.Add(p));
                return options;
            }
            catch (Exception ex) {
                WriteLog(LogLevel.Debug, $@"Error while trying to generate json serializer options.{ex.ToString()}");
                return options;
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
        protected (string authority, string pathQuery) ParseURI(string input_url) {
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
        protected (string authority, string pathQuery) ParseURI(Uri input_uri) {
            return ParseURI(input_uri.AbsoluteUri);
        }
        protected Uri CreateURI(string address) {
            bool result = Uri.TryCreate(address, UriKind.Absolute, out var uri_result)
                && (uri_result.Scheme == Uri.UriSchemeHttp || uri_result.Scheme == Uri.UriSchemeHttps);
            if (result) return uri_result;
            Console.WriteLine($@"ERROR: Unable to create URI from the address {address}");
            return null;
        }
        #endregion

        #region Abstract Methods
        public abstract IRestBase WithEndPoint(string resource_url_endpoint);
        public abstract IRestBase AddCancellationToken(CancellationToken cancellation_token);
        public abstract IRestBase WithParameter(RequestObject param);
        public abstract IRestBase WithBody(object content, bool is_serialized, BodyContentType content_type);
        public abstract IRestBase WithParameters(IEnumerable<RequestObject> parameters);
        public abstract IRestBase WithContent(HttpContent content);
        public abstract IRestBase WithQuery(QueryParam param);
        public abstract IRestBase WithQueries(IEnumerable<QueryParam> parameters);
        public abstract Task<RestResponse<T>> GetAsync<T>() where T : class;
        public abstract Task<IResponse> GetAsync();
        public abstract Task<IResponse> PostAsync();
        public abstract Task<IResponse> PutAsync();
        public abstract Task<IResponse> DeleteAsync();
        public abstract Task<IResponse> SendAsync(Method method);
        #endregion
    }
}
