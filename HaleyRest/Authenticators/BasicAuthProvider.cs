using Haley.Abstractions;
using System;
using System.Net.Http;
using System.Text;

namespace Haley.Utils {
    public class BasicAuthProvider : IAuthProvider {
        string _userName;
        string _password;
        public string GetToken() {
            if (string.IsNullOrWhiteSpace(_userName) || string.IsNullOrWhiteSpace(_password)) return null;
            var sbldr = new StringBuilder();
            sbldr.Append(_userName);
            sbldr.Append(":");
            sbldr.Append(_password);
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(sbldr.ToString()));
            return $@"Basic {token}";
        }
        public string GenerateToken(Uri baseuri, HttpRequestMessage request, IAuthParam param) {
            return GetToken();
        }

        public IAuthProvider SetToken(string username, string password) {
            _userName = username; _password = password;
            return this;
        }

        public override string ToString() {
            return _userName + ":" + _password;
        }

        public BasicAuthProvider() { }
    }
}
