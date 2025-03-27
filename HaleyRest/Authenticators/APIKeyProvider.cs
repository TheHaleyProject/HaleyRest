using Haley.Abstractions;
using System;
using System.Net.Http;

namespace Haley.Utils {
    public class APIKeyProvider : IAuthProvider {
        string _key;
        string _value;

        public string GetToken() { return _value; }

        public string GetKey() { return _key; }

        public string GenerateToken(Uri baseuri, HttpRequestMessage request, IAuthParam param) {
            return GetToken();
        }

        public IAuthProvider SetToken(string key, string value) {
            _key = key;
            _value = value;
            return this;
        }

        public override string ToString() {
            return _key + "=" + _value;
        }

        public APIKeyProvider() { }
    }
}
