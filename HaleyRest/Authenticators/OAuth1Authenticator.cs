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
    public class OAuth1Authenticator : IAuthenticator{

        //Authenticator should only hold the secret. Each request should carry their own OAuth1Token or OAuth2Token (with callback URL, request type and everything)
        public OAuth1Token Token { get; private set; }
        public OAuth1Authenticator():this(null,null) { }
        public OAuth1Authenticator(OAuth1Token token) {
            Token = token ?? new OAuth1Token(string.Empty, string.Empty);
        }
        public OAuth1Authenticator(string consumer_key,string consumer_secret) :this(new OAuth1Token(consumer_key,consumer_secret)) {

        }
        #region Public Methods

        public OAuth1Authenticator UpdateToken(OAuth1Token token) {
            Token = token ?? new OAuth1Token(string.Empty, string.Empty);
            return this;
        }
        private OAuth1Token GetToken() {
            if (Token == null) Token = new OAuth1Token();
            return Token;
        }
        public OAuth1Authenticator UpdateConsumerSecret(string consumer_key, string consumer_secret) {
            //When consumer secret changes, we need to reset the secret itself.
            var token = GetToken();
            token.UpdateSecret(new OAuthSecret(consumer_key, consumer_secret));
            return this;
        }
        public OAuth1Authenticator UpdateTokenSecret(string access_token_key, string access_token_secret) {
            var token = GetToken();
            //When token secret changes, we merely update the info.
            token.Secret.UpdateTokenInfo(access_token_key, access_token_secret);
            return this;
        }

        public string GenerateToken(Uri baseuri, HttpRequestMessage request,object auth_param) {
            if (request == null) throw new ArgumentNullException(nameof(HttpRequestMessage));
            //When a request is also attached, we should overwrite the parameters in auth_param (like request url, parameters)
            var tokenParam = auth_param as OAuth1TokenParam;
            if (tokenParam == null) tokenParam = new OAuth1TokenParam() {
                RequestType = OAuthRequestType.ForProtectedResource,
            };

            tokenParam.RequestURL = new Uri(baseuri ,request.RequestUri).ToString();
            tokenParam.Method = request.Method;
            tokenParam.Parameters = new SortedDictionary<string, string>(request.Headers.ToDictionary(p => p.Key, q => string.Join(";", q.Value))); //Since header is supposed to have Ienumerable<string> as value.
            //We use the request to fetch the headers and body before processing.
            return GetAuthorizationHeader(Token,tokenParam);
        }

        public string GetAuthorizationHeader(OAuth1Token tokeninfo, OAuth1TokenParam tokenParam) {
            ValidateInputs(tokeninfo, tokenParam);
            var headerParams = GenerateHeaderParams(tokeninfo, tokenParam);
            var basestring = GenerateBaseString(headerParams, tokeninfo, tokenParam);
            var signature = GenerateSignature(tokeninfo, basestring);
            headerParams.Add(RestConstants.OAuth.Signature, signature);
            return WriteAuthHeader(headerParams, tokeninfo.Prefix);
        }
        #endregion

        #region Private Methods
        private void ValidateInputs(OAuth1Token tokeninfo, OAuth1TokenParam tokenParam) {
            //To generate a signature, irrespective of whether we send the secret across or not, we need, ConsumerKey and Consumer secret.
            if (tokeninfo == null) throw new ArgumentNullException(nameof(OAuth1Token));
            if (tokeninfo.Secret == null) throw new ArgumentNullException(nameof(OAuth1Token.Secret));
            if (string.IsNullOrWhiteSpace(tokeninfo.Secret.ConsumerKey)) throw new ArgumentNullException(nameof(OAuth1Token.Secret.ConsumerKey));
            if (string.IsNullOrWhiteSpace(tokeninfo.Secret.ConsumerSecret)) throw new ArgumentNullException(nameof(OAuth1Token.Secret.ConsumerSecret));
            if (string.IsNullOrWhiteSpace(tokenParam?.RequestURL)) throw new ArgumentNullException(nameof(OAuth1TokenParam.RequestURL));

            switch (tokenParam.RequestType) {
                case OAuthRequestType.AccessToken:
                    //If the request is for access token, then we need both the key and secret for the access token
                    if (string.IsNullOrWhiteSpace(tokeninfo.Secret.TokenKey)) throw new ArgumentNullException(nameof(OAuth1Token.Secret.TokenKey));
                    if (string.IsNullOrWhiteSpace(tokeninfo.Secret.TokenSecret)) throw new ArgumentNullException(nameof(OAuth1Token.Secret.TokenSecret));
                    break;
                default:
                    break;
            }
        }
        private string WriteAuthHeader(Dictionary<string,string> param_list,string prefix) {
            StringBuilder sb = new StringBuilder();

            prefix = string.IsNullOrWhiteSpace(prefix) ? string.Empty : $@"{prefix.Trim()} "; //prefix followed by a space
            //Donot include items which are not required in the header.
            foreach (var item in param_list) {
                sb.AppendFormat("{0}=\"{1}\",", item.Key, item.Value);
            }
            return sb.ToString();
        }
        private string GenerateHash(string input,HashAlgorithm algorithm) {
            //var byteArray = Encoding.UTF8.GetBytes(input);
            var byteArray = Encoding.ASCII.GetBytes(input);
            var hash = algorithm.ComputeHash(byteArray); //algo will already contain the key required.
            return Convert.ToBase64String(hash);
        }

        private string GenerateSignature(OAuth1Token tokeninfo,string base_string) {
            string result = String.Empty;

            //Check : https://developer.twitter.com/en/docs/authentication/oauth-1-0a/creating-a-signature

            //Prepare
            var cons_secret = Uri.EscapeDataString(tokeninfo.Secret.ConsumerSecret);
            var token_secret = Uri.EscapeDataString(tokeninfo.Secret.TokenSecret ?? string.Empty); //Cannot escape null value so change to empty.
            var _key = string.Concat(cons_secret, "&", token_secret); //Use both secrets for signing

            //Note that there are some flows, such as when obtaining a request token, where the token secret is not yet known. In this case, the signing key should consist of the percent encoded consumer secret followed by an ampersand character ‘&’.

            //Encrypt
            switch (tokeninfo.SignatureType) {
                case SignatureType.HMACSHA1:
                    var hmac = new HMACSHA1();
                    //hmac.Key = Encoding.UTF8.GetBytes(_key); //Key as a byte array
                    hmac.Key = Encoding.ASCII.GetBytes(_key); //Key as a byte array
                    result = GenerateHash(base_string, hmac);
                    break;
                case SignatureType.HMACSHA256:
                case SignatureType.HMACSHA512:
                case SignatureType.RSASHA1:
                case SignatureType.RSASHA256:
                case SignatureType.RSASHA512:
                case SignatureType.PLAINTEXT:
                default:
                    break;
            }
            return Uri.EscapeDataString(result);
        }

        private string GenerateBaseString(Dictionary<string,string> dic, OAuth1Token tokeninfo,OAuth1TokenParam tokenpparam) {
            //Concatenate all the header params to generate the base string that will be signed in next steps.

            //1.HTTP Method name followed by ampersand
            StringBuilder sb = new StringBuilder(tokenpparam.Method.ToString().ToUpper()); //Get the HTTP Method name
            sb.Append("&");

            //2. Reqeust URL (percent encoded) followed by ampersand
            sb.Append(Uri.EscapeDataString(new Uri(tokenpparam.RequestURL).AbsoluteUri)); //can also directly give the string.
            sb.Append("&");

            //3. Append other parameters alphabetically. (Ensure all values have some value)
            var i = 1;
            foreach (var item in dic) {
                sb.Append(Uri.EscapeDataString($@"{item.Key.ToLower()}={item.Value}"));
                if (i < dic.Count) sb.Append("&");
                i++;
            }
            return sb.ToString();
        }

        private Dictionary<string, string> GenerateHeaderParams(OAuth1Token tokeninfo, OAuth1TokenParam tokenpparam) {
            var unixtimestamp = NetUtils.GetTimeStamp();
            var nonce = NetUtils.GetNonce(32);

            SortedDictionary<string, string> result = new SortedDictionary<string, string>(); //Sorted information is mandatory

            //Mandatory
            result.Add(RestConstants.OAuth.ConsumerKey, tokeninfo.Secret.ConsumerKey);
            result.Add(RestConstants.OAuth.Nonce, nonce);
            result.Add(RestConstants.OAuth.TimeStamp, unixtimestamp);
            result.Add(RestConstants.OAuth.SignatureMethod, tokeninfo.SignatureType.GetDescription());
            result.Add(RestConstants.OAuth.Version, tokeninfo.Version);

            //Optional
            if (!string.IsNullOrWhiteSpace(tokenpparam.Verifier)) {
                result.Add(RestConstants.OAuth.Verifier, tokenpparam.Verifier);
            }

            if (!string.IsNullOrWhiteSpace(tokenpparam.CallBackURL)) {
                result.Add(RestConstants.OAuth.Callback, tokenpparam.CallBackURL);
            }

            if (!string.IsNullOrWhiteSpace(tokenpparam.SessionHandle)) {
                result.Add(RestConstants.OAuth.SessionHandle, tokenpparam.SessionHandle);
            }

            //Type based

            switch (tokenpparam.RequestType) {
                case OAuthRequestType.AccessToken:
                    result.Add(RestConstants.OAuth.Token, tokeninfo.Secret.TokenKey);
                    break;
                default:
                    break;
            }
            return result.ToDictionary(p=> p.Key, p=> p.Value);
        }

        #endregion
    }
}
