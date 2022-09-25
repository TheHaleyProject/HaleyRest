using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Haley.Models
{
    public class OAuthToken {
        public string ConsumerKey { get; }
        public string ConsumerSecret { get; }
        public string TokenKey { get; private set; }
        public string TokenSecret { get;private set; }
        public string Verifier { get; private set; }
        public string Version { get; set; }
        public SignatureType SignatureType { get; set; }
        public OAuthToken SetVerifier(string verifier) {
            if (string.IsNullOrWhiteSpace(Verifier)) Verifier = verifier;
            return this;
        }
        public OAuthToken UpdateTokenInfo(string token_key, string token_secret) {
            TokenKey = token_key ?? string.Empty;
            TokenSecret = token_secret ?? string.Empty;
            return this;
        }
        public OAuthToken(string consumer_key, string consumer_secret, string token_key, string token_secret) {
            TokenKey = token_key ?? string.Empty;
            TokenSecret = token_secret ?? string.Empty;
            ConsumerKey = consumer_key;
            ConsumerSecret = consumer_secret;
            SignatureType = SignatureType.HMACSHA1;
            Version = "1.0";
        }
        public OAuthToken(string consumer_key, string consumer_secret):this(consumer_key,consumer_secret,null,null) {
            
        }
    }
}
