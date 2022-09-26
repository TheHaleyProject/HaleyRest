using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Utils
{
    public class RestConstants
    {
        public class OAuth
        {
            public const string ConsumerKey = "oauth_consumer_key";
            public const string Nonce = "oauth_nonce";
            public const string SignatureMethod = "oauth_signature_method";
            public const string TimeStamp = "oauth_timestamp";
            public const string Version = "oauth_version";
            public const string Token = "oauth_token";
            public const string Callback = "oauth_callback";
            public const string Verifier = "oauth_verifier";
            public const string SessionHandle = "oauth_session_handle";
            public const string Signature = "oauth_signature";
        }

        public class Headers {
            public const string Host = "Host";
            public const string Accept = "Accept";
            public const string AcceptEncoding = "Accept-Encoding";
            public const string AcceptLanguage = "Accept-Language";
            public const string SetCookie = "Set-Cookie";
            public const string UserAgent = "User-Agent";
            public const string Authorization = "Authorization";
            public const string ContentType = "Content-Type";
            public const string CacheControl = "Cache-Control";
            public const string Cookie = "Cookie";
            public const string Connection = "Connection";
            public const string ProxyAuthenticate = "Proxy-Authenticate";
            public const string ProxyAuthorization = "Proxy-Authorization";
        }
    }
}
