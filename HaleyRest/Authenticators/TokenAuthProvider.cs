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
        private string _token_prefix = "Bearer";
        public string GenerateToken(Uri baseuri, HttpRequestMessage request,object param) {
            //Param will override the prefix.
            var _prefix = _token_prefix;
            if (param != null && param is string prefstr) {
                _prefix = prefstr;
            }
            //For jwt we dont' do anythign with the request.
            return GetToken(_prefix);
        }

        public IAuthProvider SetToken(string token, string token_prefix) {
            if (token == null) throw new ArgumentNullException(nameof(token));
            _token = token;
            _token_prefix = token_prefix;
            return this;
        }

        private string GetToken(string prefix) {
            var result = string.Concat(prefix ?? string.Empty, " ", _token ?? string.Empty);
            return result.Trim();
        }
        
        public TokenAuthProvider() { }
    }
}
