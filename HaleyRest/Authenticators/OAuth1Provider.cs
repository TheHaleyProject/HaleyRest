using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Haley.Utils {
    //FOLLOWS : https://www.rfc-editor.org/rfc/rfc5849

    //REFERENCES:
    //https://www.chilkatsoft.com/refdoc/csOAuth1Ref.html
    //https://help.akana.com/content/current/cm/api_oauth/aaref/Ref_OAuth_AuthorizationHeader_10a.htm
    //https://developer.twitter.com/en/docs/authentication/oauth-1-0a

    //Follow: RFC-5849
    public class OAuth1Provider : IAuthProvider {

        //Authenticator should only hold the consumer key and secret. Each request should carry their own OAuth1RequestInfo(with callback URL, request type and access_token, temporary token etc)
        //reason is when multiple users are trying to use the app (which might be hosted in the server), only the consumer key/secret will be common but for each user the access_token will be different. So it is logical to split it accordingly
        private Encoding _encoding = Encoding.UTF8;
        public OAuth1Provider() : this(null, null) { }

        public OAuth1Provider(OAuth1ConsumerInfo consumer_param) {
            Consumer = consumer_param ?? new OAuth1ConsumerInfo(string.Empty, string.Empty);
        }

        public OAuth1Provider(string consumer_key, string consumer_secret) : this(new OAuth1ConsumerInfo(consumer_key, consumer_secret)) {
        }

        public OAuth1ConsumerInfo Consumer { get; private set; }

        public Encoding Encoding {
            get { return _encoding; }
            set { _encoding = value; }
        }
        #region Public Methods

        public string GenerateToken(Uri baseuri, HttpRequestMessage request, IAuthParam auth_param) {
            return GenerateTokenAsync(baseuri, request, auth_param).Result;
        }

        public async Task<string> GenerateTokenAsync(Uri baseuri, HttpRequestMessage request, IAuthParam auth_param) {
            if (request == null) throw new ArgumentNullException(nameof(HttpRequestMessage));
            //When a request is also attached, we should overwrite the parameters in auth_param (like request url, parameters)

            OAuth1RequestInfoEx requestInfo = new OAuth1RequestInfoEx() { RequestType = OAuthRequestType.RequestToken };

            //If we are trying to get the Temporary request token for first time, the requestToken will be empty ( as it is not generated yet).
            //If not empty, then we map the incoming properties of the oauthrequestinfo
            if (auth_param is OAuth1RequestInfo authreq) {
                authreq.MapProperties(requestInfo); //Token is ready only, 
            }

            //Fill the URL, Method, and parameters.
            requestInfo.RequestURL = new Uri(baseuri, request.RequestUri);
            requestInfo.Method = request.Method;

            if (requestInfo.QueryParams == null) requestInfo.QueryParams = new QueryParamList();

            //The query parameters should have already been added to the HttpRequestMessage by now. So extract it out so that we can prepare a Header with it.
            var extractedParams = await ExtractParams(request); //Extracted query params (from both body & URL should be decoded to actual final desired value. As this will be again encoded)
            if (extractedParams != null) {
                requestInfo.QueryParams.AddRange(extractedParams);
            }

            //We use the request to fetch the headers and body before processing.
            return GetAuthorizationHeader(Consumer, requestInfo);
        }

        public string GetAuthorizationHeader(OAuth1ConsumerInfo consumerInfo, OAuth1RequestInfoEx requestInfo) {
            ValidateInputs(consumerInfo, requestInfo);
            QueryParamList paramlist = new QueryParamList();

            //1.Generate oauth_ protocol parameters
            var headerParams = GenerateHeaderParams(consumerInfo, requestInfo);
            paramlist.AddDictionary(headerParams); //Add all headers.

            //2.Fetch other parameters
            paramlist.AddRange(requestInfo?.QueryParams.ToList()); //URL queries and body queries

            //3.Generate the base string
            var basestring = GenerateBaseString(paramlist, consumerInfo, requestInfo);

            //4.Prepare Header without signature and with only required parameters.
            var _header_base = WriteAuthHeader(paramlist);

            //5.Prepare signature based on the information available
            //var signature = NetUtils.UrlEncodeRelaxed(GenerateSignature(consumerInfo, basestring));
            var signature = Uri.EscapeDataString(GenerateSignature(consumerInfo, requestInfo, basestring));

            //6. Attach signature
            string auth_header = $@"{consumerInfo.Prefix.Trim()} {_header_base},{RestConstants.OAuth.Signature}=""{signature}""";
            return auth_header;
        }

        public OAuth1Provider UpdateConsumer(OAuth1ConsumerInfo token) {
            Consumer = token ?? new OAuth1ConsumerInfo(string.Empty, string.Empty);
            return this;
        }
        public OAuth1Provider UpdateConsumer(string consumer_key, string consumer_secret) {
            //When consumer secret changes, we need to reset the secret itself.
            var consumer = GetConsumer();
            consumer.UpdateToken(new OAuthToken(consumer_key, consumer_secret));
            return this;
        }

        private void UnEscapeOneLevel(ref List<QueryParam> queryParams) {
            if (queryParams != null) {
                queryParams.ForEach(p => {
                    if (!string.IsNullOrWhiteSpace(p.Key)) {
                        p.Key = Uri.UnescapeDataString(p.Key); //Unescape only once
                    }
                    if (!string.IsNullOrWhiteSpace(p.Value)) {
                        p.UpdateValue(Uri.UnescapeDataString(p.Value)); //Unescape only once.
                    }
                    p.SetAsURLDecoded(); //Now this is marked as the final desired output.
                });
            }
        }

        private async Task<List<QueryParam>> ExtractParams(HttpRequestMessage request) {

            //[RFC5489] Section: 3.4.1.3 Request Parameters
            var result = new List<QueryParam>();
            //SECTION 1: QUERY
            //The query component of the HTTP request URI as defined by [RFC3986], Section 3.4.The query component is parsed into a list of name/ value pairs by treating it as an "application/x-www-form-urlencoded" string, separating the names and values and decoding them as defined by [W3C.REC - html40 - 19980424], Section 17.13.4.
            var urlParams = NetUtils.ParseQueryParameters(request: request, ignore_prefix: "oauth_");

            if (urlParams != null) {
                //It is to be noted that whatever we receive from header is considered as encoded.
                //So we need to decode one level and set as URLDecoded (which will give us the final required output)
                UnEscapeOneLevel(ref urlParams);
                result.AddRange(urlParams);
            }


            //SECTION 2: URL ENCODED BODY ENTITY
            // The HTTP request entity-body, but only if all of the following conditions are met:
            // 1 - The entity - body is single - part.
            // 2 -  The entity - body follows the encoding requirements of the "application/x-www-form-urlencoded" content - type as defined by [W3C.REC - html40 - 19980424].
            // 3 - The HTTP request entity - header includes the "Content-Type" header field set to "application/x-www-form-urlencoded".
            // The entity-body is parsed into a list of decoded name / value pairs as described in [W3C.REC-html40 - 19980424], Section 17.13.4.
            var bodyParams = await NetUtils.ParseFormEncodedParameters(request: request, ignore_prefix: "oauth_");
            if (bodyParams != null) {
                UnEscapeOneLevel(ref bodyParams);
                result.AddRange(bodyParams);
            }

            return result;
        }

        private OAuth1ConsumerInfo GetConsumer() {
            if (Consumer == null) Consumer = new OAuth1ConsumerInfo();
            return Consumer;
        }
        #endregion

        #region Private Methods
        private string GenerateBaseString(QueryParamList paramList, OAuth1ConsumerInfo consumerInfo, OAuth1RequestInfoEx requestInfo) {
            // Ream/Signature should be excluded from the base string.

            //NOTE: AT THE END, ONLY TWO & SIGN SHOULD BE PRESENT. BASICALLY THIS SHOULD GIVE US THREE PARTS, HTTP METHOD NAME, REQUEST URL AND ENCODED PARAMS.

            //Base string
            //Concatenate all the header params to generate the base string that will be signed in next steps.

            //PART 1.HTTP Method name followed by ampersand
            StringBuilder sb = new StringBuilder(requestInfo.Method.ToString().ToUpper()); //Get the HTTP Method name
            sb.Append("&");

            //PART 2. Reqeust URL (percent encoded) followed by ampersand
            //sb.Append(NetUtils.UrlEncodeRelaxed(NetUtils.OAuthUtils.ConstructRequestUrl(requestInfo.RequestURL))); //can also directly give the string.
            sb.Append(Uri.EscapeDataString(NetUtils.OAuth.ConstructRequestUrl(requestInfo.RequestURL))); //can also directly give the string.
            sb.Append("&");

            //PART 3. Append other parameters alphabetically. (Ensure all values have some value)
            //the values of the params should be encoded as well. (values will be encoded first, then the whole generate string will be encoded)
            sb.Append(Uri.EscapeDataString(paramList.GetConcatenatedString(urlEncode: true))); //Get concated string of the params 
            return sb.ToString();
        }

        private string GenerateHash(string input, HashAlgorithm algorithm) {
            var byteArray = Encoding.GetBytes(input);
            //var byteArray = Encoding.ASCII.GetBytes(input);
            var hash = algorithm.ComputeHash(byteArray); //algo will already contain the key required.
            return Convert.ToBase64String(hash);
        }

        private Dictionary<string, string> GenerateHeaderParams(OAuth1ConsumerInfo consumerInfo, OAuth1RequestInfo requestInfo) {

            //As specified in Section 3.1 of RFC5849 
            var unixtimestamp = NetUtils.OAuth.GetTimeStamp();
            var nonce = NetUtils.OAuth.GetNonce();

            //SIGNATURE SHOULD NOT BE GENERATED HERE.
            SortedDictionary<string, string> result = new SortedDictionary<string, string>(); //Sorted information is mandatory
            //Mandatory

            result.Add(RestConstants.OAuth.ConsumerKey, consumerInfo.Token.Key);
            result.Add(RestConstants.OAuth.Nonce, nonce);
            result.Add(RestConstants.OAuth.TimeStamp, unixtimestamp);
            result.Add(RestConstants.OAuth.SignatureMethod, consumerInfo.SignatureType.GetDescription());
            result.Add(RestConstants.OAuth.Version, consumerInfo.Version);

            //Optional
            if (!string.IsNullOrWhiteSpace(requestInfo.Verifier)) {
                result.Add(RestConstants.OAuth.Verifier, requestInfo.Verifier);
            }

            if (!string.IsNullOrWhiteSpace(requestInfo.CallBackURL?.ToString())) {
                result.Add(RestConstants.OAuth.Callback, requestInfo.CallBackURL.ToString());
            }

            if (!string.IsNullOrWhiteSpace(requestInfo.SessionHandle)) {
                result.Add(RestConstants.OAuth.SessionHandle, requestInfo.SessionHandle);
            }

            //Type based

            switch (requestInfo.RequestType) {
                case OAuthRequestType.AccessToken:
                case OAuthRequestType.ForProtectedResource:
                result.Add(RestConstants.OAuth.Token, requestInfo.Token.Key);
                break;
                default:
                break;
            }
            return result.ToDictionary(p => p.Key, p => p.Value);
        }

        private string GenerateSignature(OAuth1ConsumerInfo consumerInfo, OAuth1RequestInfo requestInfo, string base_string) {
            string result = String.Empty;

            //Check : https://developer.twitter.com/en/docs/authentication/oauth-1-0a/creating-a-signature

            //Prepare
            string cons_secret = Uri.EscapeDataString(consumerInfo.Token.Secret);
            string token_secret = "";
            if (requestInfo.Token != null && !string.IsNullOrWhiteSpace(requestInfo.Token.Secret)) {
                token_secret = Uri.EscapeDataString(requestInfo.Token.Secret); //Cannot escape null value so change to empty.
            }
            var _key = string.Concat(cons_secret, "&", token_secret); //Use both secrets for signing

            //Note that there are some flows, such as when obtaining a request token, where the token secret is not yet known. In this case, the signing key should consist of the percent encoded consumer secret followed by an ampersand character ‘&’.

            //Encrypt
            switch (consumerInfo.SignatureType) {
                case SignatureType.HMACSHA1:
                case SignatureType.HMACSHA256:
                case SignatureType.HMACSHA512:
                HMAC hmac = new HMACSHA1();
                switch (consumerInfo.SignatureType) {
                    case SignatureType.HMACSHA256:
                    hmac = new HMACSHA256();
                    break;
                    case SignatureType.HMACSHA512:
                    hmac = new HMACSHA512();
                    break;
                }
                hmac.Key = Encoding.GetBytes(_key); //Key as a byte array
                                                    //hmac.Key = Encoding.ASCII.GetBytes(_key); //Key as a byte array
                result = GenerateHash(base_string, hmac);
                break;
                case SignatureType.RSASHA1:
                using (var provider = new RSACryptoServiceProvider { PersistKeyInCsp = false }) {
                    provider.FromXmlString(consumerInfo.Token.Secret); //UnEcoded consumer secret (no token secret)
                    var hasher = SHA1.Create();
                    var hash = hasher.ComputeHash(Encoding.GetBytes(base_string));
                    result = Convert.ToBase64String(provider.SignHash(hash, CryptoConfig.MapNameToOID("SHA1")));
                }
                ;
                break;
                case SignatureType.PLAINTEXT:
                result = _key; //Direclty send the key back.
                break;
                default:
                throw new NotImplementedException("This signature signing is not implemented yet. Please try with HMAC-SHA1, HMAC-SHA256, RSA-SHA1 or PlainText.");
            }
            return result;
        }

        private void ValidateInputs(OAuth1ConsumerInfo consumerInfo, OAuth1RequestInfoEx requestInfo) {
            //To generate a signature, irrespective of whether we send the secret across or not, we need, ConsumerKey and Consumer secret.
            if (consumerInfo == null) throw new ArgumentNullException(nameof(OAuth1ConsumerInfo));
            if (consumerInfo.Token == null) throw new ArgumentNullException(nameof(OAuth1ConsumerInfo.Token));
            if (string.IsNullOrWhiteSpace(consumerInfo.Token.Key)) throw new ArgumentNullException(nameof(OAuth1ConsumerInfo.Token.Key));
            if (string.IsNullOrWhiteSpace(consumerInfo.Token.Secret)) throw new ArgumentNullException(nameof(OAuth1ConsumerInfo.Token.Secret));
            if (string.IsNullOrWhiteSpace(requestInfo?.RequestURL?.ToString())) throw new ArgumentNullException(nameof(OAuth1RequestInfoEx.RequestURL));

            switch (requestInfo.RequestType) {
                case OAuthRequestType.AccessToken:
                case OAuthRequestType.ForProtectedResource:
                //If the request is for access token, then we need both the key and secret for the access token
                if (string.IsNullOrWhiteSpace(requestInfo.Token.Key)) throw new ArgumentNullException(nameof(OAuth1RequestInfo.Token.Key));
                if (string.IsNullOrWhiteSpace(requestInfo.Token.Secret)) throw new ArgumentNullException(nameof(OAuth1RequestInfo.Token.Secret));
                break;
                default:
                break;
            }
        }
        //private string WriteAuthHeader(Dictionary<string,string> param_list,string prefix) {
        //    StringBuilder sb = new StringBuilder();

        //    prefix = string.IsNullOrWhiteSpace(prefix) ? string.Empty : $@"{prefix.Trim()} "; //prefix followed by a space
        //    //Donot include items which are not required in the header.
        //    foreach (var item in param_list) {
        //        sb.AppendFormat("{0}=\"{1}\",", item.Key, item.Value);
        //    }
        //    return sb.ToString();
        //}
        private string WriteAuthHeader(QueryParamList paramlist) {
            StringBuilder sb = new StringBuilder();

            //Donot include items which are not required in the header.
            //Auth header needs only items which starts with OAuth
            //See to it that this is not encoded , it is the final value.

            var authheaderList = paramlist.Where(p => p.Key.ToLower().StartsWith("oauth_"));

            var count = authheaderList.Count();
            int i = 0;
            foreach (var item in authheaderList) {
                sb.AppendFormat("{0}=\"{1}\"", item.Key, item.Value);
                i++;
                if (i < count) {
                    sb.Append(",");
                }
            }
            return sb.ToString();
        }

        public void ClearToken() {
            //Not applicable as OAuth1 will generate token each time for new requests (based on payload hash signature)
        }
        #endregion
    }
}
