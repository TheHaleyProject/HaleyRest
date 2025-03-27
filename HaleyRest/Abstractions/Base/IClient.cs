using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Haley.Abstractions {
    /// <summary>
    /// A simple straightforward HTTPclient Wrapper.
    /// </summary>
    public interface IClient : IRestBase, IRestBase<IClient> {
        string FriendlyName { get; }

        /// <summary>
        /// The Base HTTPClient
        /// </summary>
        HttpClient BaseClient { get; }
        IClient WithTimeOut(TimeSpan timeout);
        IClient WithRequestValidation(Func<HttpRequestMessage, Task<bool>> validationCallBack);
        Func<HttpRequestMessage, Task<bool>> GetRequestValidation();
        //A request should contain a client. We cannot directly use a request to execute this. If client is not present, then we cannot execute. So, let this be a client side call.
        Task<IResponse> SendAsync(HttpRequestMessage request);
        /// <summary>
        /// Creates an empty request
        /// </summary>
        /// <returns></returns>
        IRequest CreateRequest();
        IClient UpdateFriendlyName(string friendlyName);
        IClient AutoAuthenticateRequests(bool auto_authenticate = true);
    }
}
