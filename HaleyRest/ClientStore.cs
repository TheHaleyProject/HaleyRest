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
             result
                .ClearRequestHeaders()
                .ClearRequestAuthentication(); //Always clear the header when you try to get it via the client store.
             return result;
        }
        public static IClient GetClient(Enum @enum)
        
        {return GetClient(@enum.getKey());}

        public static IClient AddClient(Enum @enum, IClient client)
        { return AddClient(@enum.getKey(), client);}
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
        public static IClient AddClient(Enum @enum, string base_uri)
        { return AddClient(@enum.getKey(), new MicroClient(base_uri)); }

        public static IClient AddClient(string key, string base_uri)
        {return AddClient(key, new MicroClient(base_uri));}

        public static bool RemoveClient(string key)
        {
            return _clientDictionary.TryRemove(key, out var _removed);
        }
        public static bool RemoveClient(Enum @enum){ return RemoveClient(@enum.getKey()); }
    }
}
