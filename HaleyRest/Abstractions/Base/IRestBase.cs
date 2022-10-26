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

        #region Return Requests
        
        IRequest AddCancellationToken(CancellationToken cancellation_token); //Cancellation token is only for the requests.
        IRequest WithEndPoint(string resource_url_endpoint);
        //Prepare Request
        IRequest WithParameter(RequestObject param);
        IRequest WithParameters(IEnumerable<RequestObject> parameters);
        IRequest WithQuery(QueryParam param);
        IRequest WithQueries(IEnumerable<QueryParam> parameters);
        IRequest WithContent(HttpContent content);
        IRequest WithBody(object content, bool is_serialized, BodyContentType content_type);
        /// <summary>
        /// This will upload / download files in chunk of 4096 (4 kb)
        /// </summary>
        /// <param name="reporter"></param>
        /// <returns></returns>
        IRequest WithProgressReporter(IProgressReporter reporter);
        #endregion

        #region Generic Returns

        IAuthProvider GetAuthenticator();
        object GetAuthParam();
        Dictionary<string, IEnumerable<string>> GetHeaders();

        Task<RestResponse<T>> GetAsync<T>() where T : class;
        Task<IResponse> GetAsync();
        Task<IResponse> PostAsync();
        Task<IResponse> PutAsync();
        Task<IResponse> DeleteAsync();
        Task<IResponse> SendAsync(Method method);
        #endregion
    }
}
