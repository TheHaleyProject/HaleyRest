using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Haley.Models {
    public class OAuth1TokenParam {
        public HttpMethod Method { get; set; }
        public Uri CallBackURL { get; set; }
        public Uri RequestURL { get; set; }
        public string SessionHandle { get; set; }
        public OAuthRequestType RequestType { get; set; }
        public Dictionary<string,string> QueryParams { get; set; }
        public string Verifier { get; set; }
        public OAuth1TokenParam() {
            Method = HttpMethod.Get;
            RequestType = OAuthRequestType.ForProtectedResource; //
        }
    }
}
