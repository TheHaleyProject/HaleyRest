using Haley.Abstractions;
using System;
using System.Net.Http;

namespace Haley.Utils {
    public class TokenAuthProvider : IAuthProvider {
        private string _token = string.Empty;
        protected string _token_prefix = "Bearer";
        public string GenerateToken(Uri baseuri, HttpRequestMessage request, IAuthParam param) {
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
