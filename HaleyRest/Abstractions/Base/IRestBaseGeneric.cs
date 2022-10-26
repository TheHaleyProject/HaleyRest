using Haley.Enums;
using Haley.Utils;
using System;
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
using System.Text.Json.Serialization;
using Haley.Models;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using static Haley.Utils.RestConstants;

namespace Haley.Abstractions
{
    /// <summary>
    /// A simple straightforward HTTPclient Wrapper.
    /// </summary>
    public interface IRestBase<T> where T : IRestBase //Should come from either client or request
        {
        T SetAuthenticator(IAuthProvider authenticator);
        T RemoveAuthenticator();
        T SetAuthParam(object auth_param);
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
