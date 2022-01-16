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

namespace Haley.Abstractions
{
    /// <summary>
    /// A simple straightforward HTTPclient Wrapper.
    /// </summary>
    public interface IClient
    {
        string Id { get; }
        string FriendlyName { get; }
        string BaseURI { get; }
        ILogger Logger { get; }
        /// <summary>
        /// The Base HTTPClient
        /// </summary>
        HttpClient BaseClient { get; }
        /// <summary>
        /// Add a logger to the 
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <summary>
        /// Reset the client headers. (Base address will be retained).
        /// </summary>
        /// <returns></returns>
        IClient ResetClientHeaders();
        /// <summary>
        /// Clear the request headers. (Authentication will still be retained).
        /// </summary>
        /// <returns></returns>
        IClient ClearRequestHeaders();
        /// <summary>
        /// Add headers to be used with the request. Call ClearRequestHeaders to clear values.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        IClient AddRequestHeaders(string name, string value);
        /// <summary>
        /// Add headers to be used with the request. Call ClearRequestHeaders to clear values.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        IClient AddRequestHeaders(string name, List<string> values);
        /// <summary>
        /// Get the available request headers.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, IEnumerable<string>> GetRequestHeaders();
        /// <summary>
        /// Clear the request authentication values.
        /// </summary>
        /// <returns></returns>
        IClient ClearRequestAuthentication();
        /// <summary>
        /// Add authentication to be used with the request headers.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="token_prefix"></param>
        /// <returns></returns>
        IClient AddRequestAuthentication(string token, string token_prefix = "Bearer");
        /// <summary>
        /// Add authentication to the client's default header.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="token_prefix"></param>
        /// <returns></returns>
        IClient AddClientHeaderAuthentication(string token, string token_prefix = "Bearer");
        IClient AddRequestCancellationToken(CancellationToken cancellation_token);
        IClient AddJsonConverters(JsonConverter converter);
        Task<SerializedResponse<T>> GetAsync<T>(string resource_url) where T : class;
        Task<StringResponse> GetAsync(string resource_url);
        Task<StringResponse> GetAsync(string resource_url, Dictionary<string, string> parameters);
       
        Task<SerializedResponse<T>> GetAsync<T>(string resource_url, Dictionary<string, string> parameters) where T : class;
        Task<IResponse> PostAsync(string resource_url, Dictionary<string, string> dictionary);
        Task<IResponse> PostAsync(string resource_url, object content, bool is_serialized);
        Task<IResponse> PostAsync(string resource_url, RestParam param);
        Task<IResponse> PostAsync(string resource_url, IEnumerable<RestParam> param_list);
       
        Task<IResponse> SendAsync(string url, object content, Method method = Method.Get, ParamType param_type = ParamType.Default, bool is_serialized = false);
        Task<IResponse> SendAsync(string url, RestParam param, Method method = Method.Get);
        Task<IResponse> SendAsync(string url, IEnumerable<RestParam> paramList, Method method = Method.Get);
        Task<IResponse> SendAsync(string url, HttpContent content, Method method = Method.Get);
        Task<IResponse> SendAsync(HttpRequestMessage request);
        /// <summary>
        /// All calls to the client is blocked.
        /// </summary>
        /// <returns></returns>
        IClient BlockClient(string message = null);
        IClient BlockClient(double block_seconds, string message = null);

        Task BlockClientAsync(string message = null);
        Task BlockClientAsync(double block_seconds, string message = null);
        /// <summary>
        /// Client is unblocked.
        /// </summary>
        /// <returns></returns>
        IClient UnBlockClient(string message = null);
    }
}
