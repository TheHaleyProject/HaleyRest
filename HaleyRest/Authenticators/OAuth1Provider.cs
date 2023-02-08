using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Runtime;
using System.Runtime.CompilerServices;
using Haley.Models;
using Haley.Enums;
using System.Text.Json;
using System.Xml.Schema;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Data.Common;
using Haley.Abstractions;

namespace Haley.Utils
{
    //FOLLOWS : https://www.rfc-editor.org/rfc/rfc5849

    //REFERENCES:
    //https://www.chilkatsoft.com/refdoc/csOAuth1Ref.html
    //https://help.akana.com/content/current/cm/api_oauth/aaref/Ref_OAuth_AuthorizationHeader_10a.htm
    //https://developer.twitter.com/en/docs/authentication/oauth-1-0a

    //Follow: RFC-5849
    public class OAuth1Provider : IAuthProvider{

        //Authenticator should only hold the consumer key and secret. Each request should carry their own OAuth1ConsumerParam or OAuth2Token (with callback URL, request type and access_token, temporary token etc)
        //reason is when multiple users are trying to use the app (which might be hosted in the server), only the consumer key/secret will be common but for each user the access_token will be different. So it is logical to split it accordingly
        private Encoding _encoding = Encoding.UTF8;
        public Encoding Encoding {
            get { return _encoding; }
            set { _encoding = value; }
        }

        public OAuth1ConsumerInfo Consumer { get; private set; }
        public OAuth1Provider():this(null,null) { }
        public OAuth1Provider(OAuth1ConsumerInfo consumer_param) {
            Consumer = consumer_param ?? new OAuth1ConsumerInfo(string.Empty, string.Empty);
        }
        public OAuth1Provider(string consumer_key,string consumer_secret) :this(new OAuth1ConsumerInfo(consumer_key,consumer_secret)) {

        }
        #region Public Methods

        public OAuth1Provider UpdateConsumer(OAuth1ConsumerInfo token) {
            Consumer = token ?? new OAuth1ConsumerInfo(string.Empty, string.Empty);
            return this;
        }
        private OAuth1ConsumerInfo GetConsumer() {
            if (Consumer == null) Consumer = new OAuth1ConsumerInfo();
            return Consumer;
        }
        public OAuth1Provider UpdateConsumer(string consumer_key, string consumer_secret) {
            //When consumer secret changes, we need to reset the secret itself.
            var consumer = GetConsumer();
            consumer.UpdateToken(new OAuthToken(consumer_key, consumer_secret));
            return this;
        }

        public string GenerateToken(Uri baseuri, HttpRequestMessage request, IAuthParam auth_param) {
            if (request == null) throw new ArgumentNullException(nameof(HttpRequestMessage));
            //When a request is also attached, we should overwrite the parameters in auth_param (like request url, parameters)

            OAuth1RequestInfoEx requestInfo = new OAuth1RequestInfoEx() { RequestType = OAuthRequestType.RequestToken };

            bool? url_decode = false;

            if (requestInfo?.Arguments?.Length > 0) {
                url_decode = requestInfo?.Arguments[0] as bool?;
            }

          //If we are trying to get the Temporary request token for first time, the requestToken will be empty ( as it is not generated yet)
            if (auth_param is OAuth1RequestInfo authreq) {
                authreq.MapProperties(requestInfo); //Token is ready only, 
            }
          
            //Fill the URL, Method, and parameters.
            requestInfo.RequestURL = new Uri(baseuri, request.RequestUri);
            requestInfo.Method = request.Method;
            FillQueryParam(request,ref requestInfo);
            //We use the request to fetch the headers and body before processing.
            return GetAuthorizationHeader(Consumer,requestInfo);
        }

        private void FillQueryParam(HttpRequestMessage request,ref OAuth1RequestInfoEx param) {
            param.QueryParams = NetUtils.OAuth.ParseQueryParameters(request:request); //to ensure it is not null.
            //TODO: CONSIDER THE BODY AND ALSO THE URLCONDED FORMBODYREQUEST LATER.
        }
        
        public string GetAuthorizationHeader(OAuth1ConsumerInfo consumerInfo, OAuth1RequestInfoEx requestInfo) {
            ValidateInputs(consumerInfo, requestInfo);
            RestParamList paramlist = new RestParamList();

            //1.Generate oauth_ protocol parameters
            var headerParams = GenerateHeaderParams(consumerInfo, requestInfo);
            paramlist.AddDictionary(headerParams,kvp_encodestrict:false); //Add all headers.

            //2.Fetch other parameters
            paramlist.AddDictionary(requestInfo?.QueryParams,kvp_encodestrict:false); //Add all queries with values.

            //3.Generate the base string
            var basestring = GenerateBaseString(paramlist, consumerInfo, requestInfo);

            //4.Prepare Header without signature
            var _header_base = WriteAuthHeader(paramlist);

            //5.Prepare signature based on the information available
            //var signature = NetUtils.UrlEncodeRelaxed(GenerateSignature(consumerInfo, basestring));
            var signature = Uri.EscapeDataString(GenerateSignature(consumerInfo,requestInfo, basestring));

            //6. Attach signature
            string auth_header = $@"{consumerInfo.Prefix.Trim()} {_header_base},{RestConstants.OAuth.Signature}=""{signature}""";
            return auth_header;
        }
        #endregion

        #region Private Methods
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
        private string WriteAuthHeader(RestParamList paramlist) {
            StringBuilder sb = new StringBuilder();

            //Donot include items which are not required in the header.
            var count = paramlist.Count();
            int i = 0;
            foreach (var item in paramlist) {
                sb.AppendFormat("{0}=\"{1}\"", item.Key, item.Value);
                i++;
                if (i < count) {
                    sb.Append(",");
                }
            }
            return sb.ToString();
        }
        private string GenerateHash(string input,HashAlgorithm algorithm) {
            var byteArray = Encoding.GetBytes(input);
            //var byteArray = Encoding.ASCII.GetBytes(input);
            var hash = algorithm.ComputeHash(byteArray); //algo will already contain the key required.
            return Convert.ToBase64String(hash);
        }

        private string GenerateSignature(OAuth1ConsumerInfo consumerInfo,OAuth1RequestInfo requestInfo,string base_string) {
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
                        result =  Convert.ToBase64String(provider.SignHash(hash, CryptoConfig.MapNameToOID("SHA1")));
                    } ;
                    break;
                case SignatureType.PLAINTEXT:
                    result = _key; //Direclty send the key back.
                    break;
                default:
                    throw new NotImplementedException("This signature signing is not implemented yet. Please try with HMAC-SHA1, HMAC-SHA256, RSA-SHA1 or PlainText.");
            }
            return result;
        }

        private string GenerateBaseString(RestParamList paramList, OAuth1ConsumerInfo consumerInfo,OAuth1RequestInfoEx requestInfo) {
            //Concatenate all the header params to generate the base string that will be signed in next steps.

            //1.HTTP Method name followed by ampersand
            StringBuilder sb = new StringBuilder(requestInfo.Method.ToString().ToUpper()); //Get the HTTP Method name
            sb.Append("&");

            //2. Reqeust URL (percent encoded) followed by ampersand
            //sb.Append(NetUtils.UrlEncodeRelaxed(NetUtils.OAuthUtils.ConstructRequestUrl(requestInfo.RequestURL))); //can also directly give the string.
            sb.Append(Uri.EscapeDataString(NetUtils.OAuth.ConstructRequestUrl(requestInfo.RequestURL))); //can also directly give the string.
            sb.Append("&");

            //3. Append other parameters alphabetically. (Ensure all values have some value)
            //sb.Append(NetUtils.UrlEncodeRelaxed(paramList.GetConcatenatedString(encodevalues:true))); //Get concated string of the params (to do : url encoding)
            //the values of the params should be encoded as well. (values will be encoded first, then the whole generate string will be encoded)
            sb.Append(Uri.EscapeDataString(paramList.GetConcatenatedString(encodevalues:true))); //Get concated string of the params (to do : url encoding)
            return sb.ToString();
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
            return result.ToDictionary(p=> p.Key, p=> p.Value);
        }
      
        #endregion
    }
}
