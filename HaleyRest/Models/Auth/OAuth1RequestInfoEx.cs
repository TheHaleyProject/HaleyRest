using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Haley.Models {
    public class OAuth1RequestInfoEx : OAuth1RequestInfo {
        public HttpMethod Method { get; set; }
        public Uri RequestURL { get; set; }
        public QueryParamList QueryParams { get; set; }
        public OAuth1RequestInfoEx() : this(null, HttpMethod.Get, OAuthRequestType.ForProtectedResource) {
        }
        public OAuth1RequestInfoEx(OAuthToken request_token) :this(request_token,HttpMethod.Get,OAuthRequestType.ForProtectedResource) {
        }
        public OAuth1RequestInfoEx(OAuthToken request_token, HttpMethod method,OAuthRequestType request_type):base(request_token,request_type) {
            Method = method;
            QueryParams = new QueryParamList();
        }
    }
}
