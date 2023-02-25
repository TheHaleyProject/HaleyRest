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
using System.Xml.Schema;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Data.Common;
using Haley.Abstractions;

namespace Haley.Utils
{
    public class APIKeyProvider : IAuthProvider{
        string _key;
        string _value;

        public string GetToken() { return _value; }

        public string GetKey() { return _key; }

        public string GenerateToken(Uri baseuri, HttpRequestMessage request,IAuthParam param) {
            return GetToken();
        }

        public IAuthProvider SetToken(string key, string value) {
            _key = key; 
            _value = value;
            return this;
        }

        public override string ToString() {
            return _key+ "=" + _value;
        }

        public APIKeyProvider() { }
    }
}
