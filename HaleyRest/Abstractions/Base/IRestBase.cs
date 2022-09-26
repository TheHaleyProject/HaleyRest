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

namespace Haley.Abstractions
{
    /// <summary>
    /// A simple straightforward HTTPclient Wrapper.
    /// </summary>
    public interface IRestBase
    {
        string Id { get; }
        string URL { get; }
        IRestBase SetLogger(ILogger logger);
        IRestBase ResetHeaders();
        IRestBase ResetHeaders(Dictionary<string, IEnumerable<string>> reset_values);
        IRestBase AddDefaultHeaders();
        IRestBase AddHeader(string name, string value);
        IRestBase AddHeaders(string name, List<string> values);
        Dictionary<string, IEnumerable<string>> GetHeaders();
        IRestBase AddCancellationToken(CancellationToken cancellation_token);
        IRestBase AddJsonConverter(JsonConverter converter);
        IRestBase RemoveJsonConverter(JsonConverter converter);
        IRestBase ClearAuthentication();
        IRestBase SetAuthenticator(IAuthenticator authenticator);
        IRestBase WithEndPoint(string resource_url_endpoint);
        //Prepare Request
        IRestBase WithParameter(RequestObject param);
        IRestBase WithParameters(IEnumerable<RequestObject> parameters);
        IRestBase WithQuery(QueryParam param);
        IRestBase WithQueries(IEnumerable<QueryParam> parameters);
        IRestBase WithContent(HttpContent content);
        IRestBase WithBody(object content, bool is_serialized, BodyContentType content_type);

        Task<RestResponse<T>> GetAsync<T>() where T : class;
        Task<IResponse> GetAsync();
        Task<IResponse> PostAsync();
        Task<IResponse> PutAsync();
        Task<IResponse> DeleteAsync();
        Task<IResponse> SendAsync(Method method);
    }
}
