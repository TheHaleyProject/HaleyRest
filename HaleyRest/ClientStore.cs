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
using System.Collections.Concurrent;

namespace Haley.Utils
{
    public static class ClientStore
    {
        private static ConcurrentDictionary<string, MicroClient> _clientDictionary = new ConcurrentDictionary<string, MicroClient>();

        public static MicroClient GetClient(string key)
        {
             _clientDictionary.TryGetValue(key, out var result);
             return result;
        }
        public static MicroClient GetClient(Enum @enum)
        {return GetClient(@enum.getKey());}

        public static MicroClient AddClient(Enum @enum,MicroClient client)
        { return AddClient(@enum.getKey(), client);}

        public static MicroClient AddClient(string key, MicroClient client)
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
        public static MicroClient AddClient(Enum @enum, string base_uri)
        { return AddClient(@enum.getKey(), new MicroClient(base_uri)); }

        public static MicroClient AddClient(string key, string base_uri)
        {return AddClient(key, new MicroClient(base_uri));}

        public static bool RemoveClient(string key)
        {
            return _clientDictionary.TryRemove(key, out var _removed);
        }
        public static bool RemoveClient(Enum @enum){ return RemoveClient(@enum.getKey()); }
    }
}
