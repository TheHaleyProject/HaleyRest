using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Haley.Models {
    //Error IDs begin with 1000's
    /// <summary>
    /// Rest Base for both FluentClient and also RestRequest
    /// </summary>
    public abstract class RestBase : IRestBase {
        public string Id { get; }
        public string URL { get; protected set; }

        protected ConcurrentDictionary<Type, JsonConverter> _jsonConverters = new ConcurrentDictionary<Type, JsonConverter>();

        #region Attributes
        ILogger _logger;
        ConcurrentDictionary<string, IEnumerable<string>> _headers = new ConcurrentDictionary<string, IEnumerable<string>>();
        IAuthProvider _authenticator;
        IAuthParam _authParam = null;
        #endregion

        #region Constructors
        protected RestBase(string url) {
            Id = Guid.NewGuid().ToString();
            URL = url;
            //On creation add default request headers.
            ResetHeaders();
            AddDefaultHeaders();
        }
        protected RestBase() : this(string.Empty) { }
        #endregion

        #region Protected Methods
        protected IRestBase SetLogger(ILogger logger) {
            this._logger = logger;
            return this;
        }
        protected IRestBase AddJsonConverter(JsonConverter converter) {
            try {
                if (converter == null) return this;

                if (!_jsonConverters.ContainsKey(converter.GetType())) {
                    _jsonConverters.TryAdd(converter.GetType(), converter);
                }
                return this;
            } catch (Exception ex) {
                EventId _eventid = new EventId(1001, "JSONConverter Add Error");
                WriteLog(LogLevel.Trace, _eventid, "Error while trying to JSON Converter", ex);
                return this;
            }
        }
        protected IRestBase RemoveJsonConverter(JsonConverter converter) {
            try {
                if (converter == null) return this;
                var _type = converter.GetType();
                if (_jsonConverters.ContainsKey(_type)) {
                    _jsonConverters.TryRemove(_type, out var removed);
                }
                return this;
            } catch (Exception ex) {
                EventId _eventid = new EventId(1002, "JSONConverter Remove Error");
                WriteLog(LogLevel.Trace, _eventid, "Error while trying to JSON Converter", ex);
                return this;
            }
        }
        protected virtual IRestBase ResetHeaders() {
            WriteLog(LogLevel.Debug, "Clear all headers");
            _headers?.Clear();
            return this;
        }
        protected IRestBase ResetHeaders(Dictionary<string, IEnumerable<string>> reset_values) {
            if (reset_values == null || reset_values.Count == 0) {
                _headers = new ConcurrentDictionary<string, IEnumerable<string>>();
            } else {
                _headers = new ConcurrentDictionary<string, IEnumerable<string>>(reset_values);
            }
            return this;
        }
        protected IRestBase AddDefaultHeaders() {
            if (_headers == null) _headers = new ConcurrentDictionary<string, IEnumerable<string>>();
            //Add default values.
            AddHeader(RestConstants.Headers.Accept, "*/*");
            AddHeader(RestConstants.Headers.AcceptCharSet, "utf-8");
            AddHeader(RestConstants.Headers.UserAgent, "HaleyFluentClient");
            AddHeaderValues(RestConstants.Headers.AcceptEncoding, new List<string>() { "gzip", "deflate" });
            AddHeader(RestConstants.Headers.Connection, "keep-alive");
            AddHeader(RestConstants.Headers.CacheControl, "no-cache");
            return this;
        }
        protected IRestBase AddHeader(string name, string value) {
            AddHeaderValues(name, new List<string>() { value });
            return this;
        }

        protected IRestBase RemoveHeader(string name) {
            if (!string.IsNullOrWhiteSpace(name)) {
                _headers.TryRemove(name, out _);
            }
            return this;
        }

        protected IRestBase AddHeaderValues(string name, List<string> values) {
            _headers?.TryAdd(name, values);
            return this;
        }

        protected IRestBase ReplaceHeader(string name, string value) {
            ReplaceHeaderValues(name, new List<string>() { value });
            return this;
        }

        protected IRestBase ReplaceHeaderValues(string name, List<string> values) {
            if (_headers.ContainsKey(name)) {
                _headers[name] = values;
            } else {
                _headers?.TryAdd(name, values);
            }
            return this;
        }

        protected IRestBase ClearAuthentication() {
            //todo: This should not remove the authenticator but should temporarily disable authentication for this call alone. 
            _authenticator = null;
            return this;
        }
        protected IRestBase RemoveAuthenticator() {
            _authenticator = null;
            return this;
        }
        protected IRestBase SetAuthenticator(IAuthProvider authenticator) {
            _authenticator = authenticator;
            return this;
        }


        protected IRestBase SetAuthParam(IAuthParam auth_param) {
            _authParam = auth_param;
            return this;
        }
        #endregion

        #region Interface Methods
        public IAuthProvider GetAuthenticator() {
            return _authenticator;

        }
        public IAuthParam GetAuthParam() {
            return _authParam;
        }
        public Dictionary<string, IEnumerable<string>> GetHeaders() {
            return _headers.ToDictionary(p => p.Key, q => q.Value);
        }
        #endregion

        #region Helpers
        protected JsonSerializerOptions GetSerializerOptions() {
            //WriteIndented = true,
            var options = new JsonSerializerOptions() {
                //WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            };
            try {
                _jsonConverters.Values.ToList().ForEach(p => options.Converters.Add(p));
                return options;
            } catch (Exception ex) {
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
                    } else {
                        //Relative URL should not have a leading '/'
                        input_url = input_url.TrimStart('/');
                    }
                }
                return (string.Empty, input_url);
            } catch (Exception ex) {
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
        public abstract IRequest WithProgressReporter(IProgressReporter reporter);
        public abstract IRequest WithEndPoint(string resource_url_endpoint);
        public abstract IRequest AddCancellationToken(CancellationToken cancellation_token);
        public abstract IRequest AddHTTPCompletion(HttpCompletionOption completion_option);
        public abstract IRequest WithParameter(IRequestContent param);
        public abstract IRequest WithBody(object content, bool is_serialized, BodyContentType content_type);
        public abstract IRequest WithBody(IRawBodyRequestContent rawBodyRequest);
        public abstract IRequest WithParameters(IEnumerable<IRequestContent> parameters);
        public abstract IRequest WithForm(IFormRequestContent form);
        public abstract IRequest WithForm(Dictionary<string,object> formData);
        public abstract IRequest WithForm(FormUrlEncodedContent content, string key);
        public abstract IRequest WithContent(HttpContent content);
        public abstract IRequest WithQuery(IQueryRequestContent param);
        public abstract IRequest WithQueries(IEnumerable<IQueryRequestContent> parameters);
        public abstract Task<RestResponse<T>> GetAsync<T>() where T : class;
        public abstract Task<IResponse> GetAsync();
        public abstract Task<IResponse> PostAsync();
        public abstract Task<IResponse> PutAsync();
        public abstract Task<IResponse> DeleteAsync();
        public abstract Task<IResponse> SendAsync(Method method);
        #endregion
    }
}
