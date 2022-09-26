using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Runtime;
using System.Runtime.CompilerServices;
using Haley.Models;
using Haley.Enums;
using System.Text.Json;
using Haley.Utils;
using Haley.Abstractions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Haley.Rest
{
    public static class ClientStore
    {
        private static ConcurrentDictionary<string, IClient> _clientDictionary = new ConcurrentDictionary<string, IClient>();

        /// <summary>
        /// For each call, this will remove any stored request headers added before and will return the client
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IClient GetClient(string key)
        {
             _clientDictionary.TryGetValue(key, out var result);
             return result;
        }
        public static IClient GetClient(Enum @enum)
        
        {return GetClient(@enum.GetKey());}

        public static IClient AddClient(Enum @enum, IClient client)
        { return AddClient(@enum.GetKey(), client);}
        public static IClient AddClient(string key, IClient client)
        {
            if (client == null) return null;
            if (_clientDictionary.TryAdd(key, client))
            {
                return client; //If sucessfully added.
            }
            else
            {
                return GetClient(key); //if key already exists.
            }
        }
        public static IClient AddClient(Enum @enum, string base_uri, string friendly_name = null,Func<HttpRequestMessage,Task<bool>> requestvalidation = null,ILogger logger = null)
        { return AddClient(@enum.GetKey(), base_uri,friendly_name,requestvalidation, logger); }

        public static IClient AddClient(string key, string base_uri, string friendly_name = null, Func<HttpRequestMessage, Task<bool>> requestvalidation = null, ILogger logger = null)
        {return AddClient(key, new FluentClient(base_uri,friendly_name, requestvalidation,logger));}

        public static bool RemoveClient(string key)
        {
            return _clientDictionary.TryRemove(key, out var _removed);
        }
        public static bool RemoveClient(Enum @enum){ return RemoveClient(@enum.GetKey()); }
    }
}
