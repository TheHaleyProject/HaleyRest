using Haley.Enums;

namespace Haley.Models {

    //FOLLOWS : https://www.rfc-editor.org/rfc/rfc5849

    public class OAuth1ConsumerInfo {
        //Tokens can be different for each request , however the consumer_key/secret will remain the same for different request from same base_url. Assuming this, secret remains the same but token will be different (because it could be a different request URL & request type and also different accesstoken/secret based on the user trying to access it.)
        public string Prefix { get; }
        public OAuthToken Token { get; set; }
        public string Version { get; set; }
        public SignatureType SignatureType { get; set; }
        public void UpdateToken(OAuthToken token) {
            Token = token;
        }

        public OAuth1ConsumerInfo(string consumer_key, string consumer_secret) {
            Prefix = "OAuth";
            SignatureType = SignatureType.HMACSHA1;
            Version = "1.0";
            Token = new OAuthToken(consumer_key ?? string.Empty, consumer_secret ?? string.Empty);
        }
        public OAuth1ConsumerInfo() : this(null, null) {

        }
    }
}
