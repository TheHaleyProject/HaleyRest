using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class OAuth1Token
    {
        //Tokens can be different for each request , however the consumer_key/secret,token_key/secret will remain the same for different request from same base_url. Assuming this, secret remains the same but token will be different (because it could be a different request URL & request type)
        public string Prefix { get; }
        public OAuthSecret Secret { get; set; }
        public string Version { get; set; }
        public SignatureType SignatureType { get; set; }
        public void UpdateSecret(OAuthSecret secret) {
            Secret = secret;
        }

        public OAuth1Token(string consumer_key,string consumer_secret) {
            Prefix = "OAuth";
            SignatureType = SignatureType.HMACSHA1;
            Version = "1.0";
            Secret = new OAuthSecret(consumer_key ?? string.Empty, consumer_secret ?? string.Empty);
        }
        public OAuth1Token():this(null,null) {
            
        }
    }
}
