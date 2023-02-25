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
    public class TokenAuthProvider : IAuthProvider{
        private string _token = string.Empty;
        protected string _token_prefix = "Bearer";
        public string GenerateToken(Uri baseuri, HttpRequestMessage request,IAuthParam param) {
            return GetToken();
        }

        public IAuthProvider SetToken(string token) {
            if (token == null) throw new ArgumentNullException(nameof(token));
            _token = token;
            return this;
        }

        public IAuthProvider SetTokenPrefix(string token_prefix = "Bearer") {
            _token_prefix = token_prefix;
            return this;
        }

        private string GetToken() {
            var result = string.Concat(_token_prefix ?? string.Empty, " ", _token ?? string.Empty);
            return result.Trim();
        }
        
        public TokenAuthProvider() { }
    }
}
