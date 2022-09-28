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
    public class TokenAuthenticator : IAuthenticator{
        private string _token = string.Empty;
        public string GenerateToken(HttpRequestMessage request) {
            //For jwt we dont' do anythign with the request.
            return _token;
        }
        public void SetToken(string token, string token_prefix) {
            if (token == null) throw new ArgumentNullException(nameof(token));

            _token = string.Concat(token_prefix ?? "", " ", token);
            _token = _token.Trim();
        }
        
        public TokenAuthenticator() { }
    }
}
