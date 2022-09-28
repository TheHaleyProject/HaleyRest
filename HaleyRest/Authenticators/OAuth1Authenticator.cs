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

        public OAuthToken Token { get; private set; }
        public OAuthRequest Request { get; private set; }
        public Dictionary<string,string> Parameters { get; set; }
        public string Prefix { get; set; }

        public OAuth1Authenticator():this(null,null) { }
        public OAuth1Authenticator(OAuthToken tokeninfo) {
            Token = tokeninfo ?? new OAuthToken(string.Empty, string.Empty);
            Request = new OAuthRequest();
            Parameters = new Dictionary<string, string>();
            Prefix = "OAuth";
        }
        public OAuth1Authenticator(string consumer_key,string consumer_secret) :this(new OAuthToken(consumer_key,consumer_secret)) {

        }
        #region Public Methods

        public OAuth1Authenticator SetOAuthRequest(OAuthRequest request) {
            Request = request ?? new OAuthRequest();
            return this;
        }

        public OAuth1Authenticator UpdateToken(OAuthToken newToken) {
            Token = newToken ?? new OAuthToken(null,null);
            return this;
        }

        public OAuth1Authenticator UpdateToken(string consumer_key, string consumer_secret) {
            return UpdateToken(new OAuthToken(consumer_key, consumer_secret));   
        }

        public OAuth1Authenticator UpdateAccessTokenInformation(string access_token_key, string access_token_secret) {
            Token.UpdateTokenInfo(access_token_key,access_token_secret);
            return this;
        }

        public string GenerateToken(HttpRequestMessage request) {
            //We use the request to fetch the headers and body before processing.
            return GetAuthorizationHeader();
        }

        public string GetAuthorizationHeader() {
            return GetAuthorizationHeader(Token, Request, Parameters,Prefix);
        }
        public string GetAuthorizationHeader(OAuthToken tokeninfo, OAuthRequest requestInfo, Dictionary<string, string> parameters,string prefix) {
            ValidateInputs(tokeninfo, requestInfo);
            var headerParams = GenerateHeaderParams(tokeninfo, requestInfo);
            var basestring = GenerateBaseString(headerParams, tokeninfo, requestInfo);
            var signature = GenerateSignature(tokeninfo,requestInfo, basestring);
            headerParams.Add(RestConstants.OAuth.Signature, signature);
            return WriteAuthHeader(headerParams, prefix);
        }
        #endregion

        #region Private Methods
        private void ValidateInputs(OAuthToken tokeninfo, OAuthRequest requestInfo) {
            //To generate a signature, irrespective of whether we send the secret across or not, we need, ConsumerKey and Consumer secret.
            if (tokeninfo == null) throw new ArgumentNullException("OAuth Token information");
            if (string.IsNullOrWhiteSpace(tokeninfo.ConsumerKey)) throw new ArgumentNullException(nameof(OAuthToken.ConsumerKey));
            if (string.IsNullOrWhiteSpace(tokeninfo.ConsumerSecret)) throw new ArgumentNullException(nameof(OAuthToken.ConsumerSecret));
            if (string.IsNullOrWhiteSpace(requestInfo.RequestURL)) throw new ArgumentNullException(nameof(requestInfo.RequestURL));

            switch (requestInfo.RequestType) {
                case OAuthRequestType.AccessToken:
                    //If the request is for access token, then we need both the key and secret for the access token
                    if (string.IsNullOrWhiteSpace(tokeninfo.TokenKey)) throw new ArgumentNullException(nameof(OAuthToken.TokenKey));
                    if (string.IsNullOrWhiteSpace(tokeninfo.TokenSecret)) throw new ArgumentNullException(nameof(OAuthToken.TokenSecret));
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

        private string GenerateSignature(OAuthToken tokeninfo, OAuthRequest requestInfo,string base_string) {
            string result = String.Empty;

            //Check : https://developer.twitter.com/en/docs/authentication/oauth-1-0a/creating-a-signature

            //Prepare
            var cons_secret = Uri.EscapeDataString(tokeninfo.ConsumerSecret);
            var token_secret = Uri.EscapeDataString(tokeninfo.TokenSecret ?? string.Empty); //Cannot escape null value so change to empty.
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

        private string GenerateBaseString(Dictionary<string,string> dic, OAuthToken tokeninfo,OAuthRequest requestInfo) {
            //Concatenate all the header params to generate the base string that will be signed in next steps.

            //1.HTTP Method name followed by ampersand
            StringBuilder sb = new StringBuilder(requestInfo.Method.ToString().ToUpper()); //Get the HTTP Method name
            sb.Append("&");

            //2. Reqeust URL (percent encoded) followed by ampersand
            sb.Append(Uri.EscapeDataString(new Uri(requestInfo.RequestURL).AbsoluteUri)); //can also directly give the string.
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

        private Dictionary<string, string> GenerateHeaderParams(OAuthToken tokeninfo, OAuthRequest requestInfo) {
            var unixtimestamp = NetUtils.GetTimeStamp();
            var nonce = NetUtils.GetNonce(32);

            SortedDictionary<string, string> result = new SortedDictionary<string, string>(); //Sorted information is mandatory

            //Mandatory
            result.Add(RestConstants.OAuth.ConsumerKey, tokeninfo.ConsumerKey);
            result.Add(RestConstants.OAuth.Nonce, nonce);
            result.Add(RestConstants.OAuth.TimeStamp, unixtimestamp);
            result.Add(RestConstants.OAuth.SignatureMethod, tokeninfo.SignatureType.GetDescription());
            result.Add(RestConstants.OAuth.Version, tokeninfo.Version);

            //Optional
            if (!string.IsNullOrWhiteSpace(tokeninfo.Verifier)) {
                result.Add(RestConstants.OAuth.Verifier, tokeninfo.Verifier);
            }

            if (!string.IsNullOrWhiteSpace(requestInfo.CallBackURL)) {
                result.Add(RestConstants.OAuth.Callback, requestInfo.CallBackURL);
            }

            if (!string.IsNullOrWhiteSpace(requestInfo.SessionHandle)) {
                result.Add(RestConstants.OAuth.SessionHandle, requestInfo.SessionHandle);
            }

            //Type based

            switch (requestInfo.RequestType) {
                case OAuthRequestType.AccessToken:
                    result.Add(RestConstants.OAuth.Token, tokeninfo.TokenKey);
                    break;
                default:
                    break;
            }
            return result.ToDictionary(p=> p.Key, p=> p.Value);
        }

        #endregion
    }
}
