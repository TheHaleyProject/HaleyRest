using Haley.Abstractions;
using System;
using System.Net.Http;

namespace Haley.Utils {
    public class TokenAuthProvider : IAuthProvider {
        protected string _token = string.Empty;
        private string _value = string.Empty;
        protected string _token_prefix = "Bearer";
        public string GenerateToken(Uri baseuri, HttpRequestMessage request, IAuthParam param) {
            return GetToken();
        }

        public IAuthProvider SetToken(string token) {
            if (token == null) throw new ArgumentNullException(nameof(token));
            _value = token;
            SetTokenInternal();
            return this;
        }

        public IAuthProvider SetTokenPrefix(string token_prefix = "Bearer") {
            _token_prefix = token_prefix;
            SetTokenInternal();
            return this;
        }

        void SetTokenInternal() {
            _token = string.Concat(_token_prefix ?? string.Empty, " ", _value ?? string.Empty);
            _token = _token.Trim();
        }

        private string GetToken() {
            return _token;
        }

        public void ClearToken() {
            _token = string.Empty;
        }

        public TokenAuthProvider() { }
    }
}
