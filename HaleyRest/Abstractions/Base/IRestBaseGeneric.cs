using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Haley.Abstractions {
    /// <summary>
    /// A simple straightforward HTTPclient Wrapper.
    /// </summary>
    public interface IRestBase<T> where T : IRestBase //Should come from either client or request
        {
        T SetAuthenticator(IAuthProvider authenticator);
        T RemoveAuthenticator();
        T SetAuthParam(IAuthParam auth_param);
        T ResetHeaders();
        T ResetHeaders(Dictionary<string, IEnumerable<string>> reset_values);
        T AddDefaultHeaders();
        T RemoveHeader(string name);
        T AddHeader(string name, string value);
        T AddHeaderValues(string name, List<string> values);
        T ReplaceHeader(string name, string value);
        T ReplaceHeaderValues(string name, List<string> values);
        T AddJsonConverter(JsonConverter converter);
        T RemoveJsonConverter(JsonConverter converter);
        T SetLogger(ILogger logger);
    }
}
