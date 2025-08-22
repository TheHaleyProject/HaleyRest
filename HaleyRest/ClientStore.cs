using Haley.Abstractions;
using Haley.Models;
using Haley.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

namespace Haley.Rest {
    public static class ClientStore {
        private static ConcurrentDictionary<string, IClient> _clientDictionary = new ConcurrentDictionary<string, IClient>();

        /// <summary>
        /// For each call, this will remove any stored request headers added before and will return the client
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IClient GetClient(string key) {
            _clientDictionary.TryGetValue(key, out var result);
            return result;
        }
        public static IClient GetClient(Enum @enum) {
            return GetClient(@enum.GetKey());
        }

        public static IClient AddClient(Enum @enum, IClient client) { return AddClient(@enum.GetKey(), client); }
        public static IClient AddClient(string key, IClient client) {
            if (client == null) return null;
            if (_clientDictionary.TryAdd(key, client)) {
                return client; //If sucessfully added.
            } else {
                return GetClient(key); //if key already exists.
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
