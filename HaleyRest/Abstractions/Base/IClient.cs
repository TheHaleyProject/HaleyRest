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
    public interface IClient : IRestBase
    {
        string FriendlyName { get; }
        /// <summary>
        /// The Base HTTPClient
        /// </summary>
        HttpClient BaseClient { get; }
        IClient WithRequestValidation(Func<HttpRequestMessage, Task<bool>> validationCallBack);
        Func<HttpRequestMessage, Task<bool>> GetRequestValidation();
        //A request should contain a client. We cannot directly use a request to execute this. If client is not present, then we cannot execute. So, let this be a client side call.
        Task<IResponse> SendAsync(HttpRequestMessage request);
        /// <summary>
        /// Creates an empty request
        /// </summary>
        /// <returns></returns>
        IRestBase CreateRequest();
        IClient UpdateFriendlyName(string friendlyName);
    }
}
