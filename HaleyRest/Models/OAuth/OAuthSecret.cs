using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Haley.Models
{
    //FOLLOWS : https://www.rfc-editor.org/rfc/rfc5849
    public class OAuthSecret {
        public string ConsumerKey { get; }
        public string ConsumerSecret { get; }
        public string TokenKey { get; private set; }
        public string TokenSecret { get;private set; }
    
        public OAuthSecret UpdateTokenInfo(string token_key, string token_secret) {
            TokenKey = token_key ?? string.Empty;
            TokenSecret = token_secret ?? string.Empty;
            return this;
        }
        public OAuthSecret(string consumer_key, string consumer_secret, string token_key, string token_secret) {
            TokenKey = token_key ?? string.Empty;
            TokenSecret = token_secret ?? string.Empty;
            ConsumerKey = consumer_key;
            ConsumerSecret = consumer_secret;
          
        }
        public OAuthSecret(string consumer_key, string consumer_secret):this(consumer_key,consumer_secret,null,null) {
            
        }
    }
}
