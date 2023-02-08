using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Haley.Models {
    public class OAuth1RequestInfo : IAuthParam {
        public object[] Arguments { get; set; }
        public OAuthToken Token { get; internal set; }
        public Uri CallBackURL { get; set; }
        public string SessionHandle { get; set; }
        public OAuthRequestType RequestType { get; set; }
        public string Verifier { get; set; }
        public OAuth1RequestInfo() : this(null, OAuthRequestType.ForProtectedResource) {
        }
        public OAuth1RequestInfo(OAuthToken request_token) :this(request_token,OAuthRequestType.ForProtectedResource) {
        }
        public OAuth1RequestInfo(OAuthToken request_token, OAuthRequestType request_type) {
            Token = request_token ?? new OAuthToken();
            RequestType = request_type; //
        }
    }
}
