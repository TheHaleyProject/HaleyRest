using Haley.Enums;
using Haley.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Haley.Abstractions {
    /// <summary>
    /// A simple straightforward HTTPclient Wrapper.
    /// </summary>
    public interface IRestBase {
        string Id { get; }
        string URL { get; }

        #region Return Requests

        IRequest AddCancellationToken(CancellationToken cancellation_token); //Cancellation token is only for the requests.
        IRequest AddHTTPCompletion(HttpCompletionOption completion_option); //Cancellation token is only for the requests.
        IRequest WithEndPoint(string resource_url_endpoint);
        //Prepare Request
        IRequest WithParameter(IRequestContent param);
        IRequest WithParameters(IEnumerable<IRequestContent> parameters);
        IRequest WithForm(IFormRequestContent form);
        IRequest WithForm(FormUrlEncodedContent content,string key);
        IRequest WithForm(Dictionary<string,object> formdata);
        IRequest WithQuery(IQueryRequestContent param);
        IRequest WithQueries(IEnumerable<IQueryRequestContent> parameters);
        IRequest WithContent(HttpContent content);
        IRequest WithBody(object content, bool is_serialized, BodyContentType content_type);
        IRequest WithBody(IRawBodyRequestContent rawBodyRequest);
        /// <summary>
        /// This will upload / download files in chunk of 4096 (4 kb)
        /// </summary>
        /// <param name="reporter"></param>
        /// <returns></returns>
        IRequest WithProgressReporter(IProgressReporter reporter);
        #endregion

        #region Generic Returns

        IAuthProvider GetAuthenticator();
        IAuthParam GetAuthParam();
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
