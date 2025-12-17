using Haley.Abstractions;
using Haley.Models;
using Haley.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Security.Authentication;
using System.Net.Security;

namespace Haley.Rest {
    public class ClientStore {
        private static ConcurrentDictionary<string, IClient> _clientDictionary = new ConcurrentDictionary<string, IClient>();
        private static ClientStore _instance = new ClientStore(); //static item.

        private ClientStore() { } //Private initialization

        public IClient this[string key] {
            get {
                return Get(key);
            }
        }

        public IClient this[Enum @key] {
            get {
                return Get(@key);
            }
        }


        /// <summary>
        /// For each call, this will remove any stored request headers added before and will return the client
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Obsolete("Replace with simpler Get()",true)]
        public static IClient GetClient(string key) {
            _clientDictionary.TryGetValue(key, out var result);
            return result;
        }

        [Obsolete("Replace with simpler Get()",true)]
        public static IClient GetClient(Enum @enum) {
            return GetClient(@enum.GetKey());
        }

        /// <summary>
        /// For each call, this will remove any stored request headers added before and will return the client
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IClient Get(string key) {
            _clientDictionary.TryGetValue(key, out var result);
            return result;
        }

        public static IClient Get(Enum @enum) {
            return Get(@enum.GetKey());
        }

        public static IClient GenerateClient(Dictionary<string, object> dic, ILogger logger, HttpMessageHandler handler = null) {
#if NET8_0_OR_GREATER
//If we are in .Net 8, we can go for SocketsHttp Directly
            if((dic.ContainsKey("http2") || dic.ContainsKey("ssl-ignore")) && handler == null) handler = new SocketsHttpHandler(); //We set handler for .net8

            //HTTP2
            if (handler is SocketsHttpHandler socksH){
             if (dic.ContainsKey("http2")){
                 socksH.EnableMultipleHttp2Connections = true;
                 socksH.PooledConnectionLifetime= TimeSpan.FromMinutes(5);
                 socksH.PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2);
                 socksH.MaxConnectionsPerServer = int.MaxValue;
                 socksH.SslOptions = null; //reset other custom options.
                 socksH.SslOptions = new SslClientAuthenticationOptions();
                 socksH.SslOptions.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                 socksH.SslOptions.ApplicationProtocols = new List<SslApplicationProtocol>{SslApplicationProtocol.Http2, SslApplicationProtocol.Http11};
                 socksH.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
             } else if (dic.ContainsKey("ssl-ignore")){
             //SSL - SOCKETS (We are already setting this in http2. So, if and only if http2 is not present, we set here.
                 socksH.SslOptions = new SslClientAuthenticationOptions();
                 socksH.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
             }
            }
#endif
            //Fall back


            if (dic.ContainsKey("ssl-ignore") && handler == null) handler = new HttpClientHandler();

            if (handler is HttpClientHandler cliH) {
                //SSL- HTTPCLIENT
                if (dic.ContainsKey("ssl-ignore")) {
                    cliH.ServerCertificateCustomValidationCallback = (m, c, ch, e) => true;
                }
            }

                var result = new FluentClient(dic.GenerateBaseURLAddress(), logger, handler, dic.ContainsKey("http2")) { };
            result.BaseClient.Timeout = TimeSpan.FromSeconds(200);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cfgInfo">Holding data like : "base=http://{{app-ip-or-url}}:{{port}}/;route={{/SOME-SUFFIX}};suffix={{SOME-ADDITIONAL-SUFFIX}};ssl-ignore;"</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IClient AddClient(Enum key, string cfgInfo, ILogger logger = null,HttpMessageHandler handler =null) {
            return AddClient(key.GetKey(), cfgInfo, logger,handler);
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cfgInfo">Holding data like : "base=http://{{app-ip-or-url}}:{{port}}/;route={{/SOME-SUFFIX}};suffix={{SOME-ADDITIONAL-SUFFIX}};ssl-ignore;forward-headers;"</param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IClient AddClient(string key, string cfgInfo, ILogger logger = null,HttpMessageHandler handler = null) {
            if (string.IsNullOrWhiteSpace(cfgInfo)) throw new ArgumentNullException($@"The configuration information is empty. Cannot proceed to create FluentClient for {key}");
            var dic = cfgInfo.ToDictionarySplit();
            return AddClient(key, GenerateClient(dic, logger,handler));
        }

        public static IClient AddClient(Enum @enum, IClient client) { return AddClient(@enum.GetKey(), client); }
        public static IClient AddClient(string key, IClient client) {
            if (client == null) return null;
            if (_clientDictionary.TryAdd(key, client)) {
                return client; //If sucessfully added.
            } else {
                return Get(key); //if key already exists.
            }
        }
        public static IClient AddClient(Enum @enum, string base_uri, string friendly_name, Func<HttpRequestMessage, Task<bool>> requestvalidation = null, ILogger logger = null, HttpMessageHandler handler = null) { return AddClient(@enum.GetKey(), base_uri, friendly_name, requestvalidation, logger,handler); }

        public static IClient AddClient(string key, string base_uri, string friendly_name, Func<HttpRequestMessage, Task<bool>> requestvalidation = null, ILogger logger = null, HttpMessageHandler handler = null) { return AddClient(key, new FluentClient(base_uri, friendly_name, requestvalidation, logger,handler)); }

        public static bool RemoveClient(string key) {
            return _clientDictionary.TryRemove(key, out var _removed);
        }
        public static bool RemoveClient(Enum @enum) { return RemoveClient(@enum.GetKey()); }

        public static void RemoveAllClients() { _clientDictionary.Clear(); }
    }
}
