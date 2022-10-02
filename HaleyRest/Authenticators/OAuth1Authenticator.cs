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

            tokenParam.RequestURL = new Uri(baseuri, request.RequestUri);
            tokenParam.Method = request.Method;
            FillQueryParam(request,ref tokenParam);
            //We use the request to fetch the headers and body before processing.
            return GetAuthorizationHeader(Token,tokenParam);
        }

        private void FillQueryParam(HttpRequestMessage request,ref OAuth1TokenParam param) {
            param.QueryParams = NetUtils.OAuth.ParseQueryParameters(request:request); //to ensure it is not null.
            //TODO: CONSIDER THE BODY AND ALSO THE URLCONDED FORM BODY LATER.
        }

        public string GetAuthorizationHeader(OAuth1Token tokeninfo, OAuth1TokenParam tokenParam) {
            ValidateInputs(tokeninfo, tokenParam);
            RestParamList paramlist = new RestParamList();

            //1.Generate oauth_ protocol parameters
            var headerParams = GenerateHeaderParams(tokeninfo, tokenParam);
            paramlist.AddDictionary(headerParams,kvp_encodestrict:false); //Add all headers.

            //2.Fetch other parameters
            paramlist.AddDictionary(tokenParam?.QueryParams,kvp_encodestrict:false); //Add all queries with values.

            //3.Generate the base string
            var basestring = GenerateBaseString(paramlist, tokeninfo, tokenParam);

            //4.Prepare Header without signature
            var _header_base = WriteAuthHeader(paramlist);

            //5.Prepare signature based on the information available
            //var signature = NetUtils.UrlEncodeRelaxed(GenerateSignature(tokeninfo, basestring));
            var signature = Uri.EscapeDataString(GenerateSignature(tokeninfo, basestring));

            //6. Attach signature
            string auth_header = $@"{tokeninfo.Prefix.Trim()} {_header_base},{RestConstants.OAuth.Signature}=""{signature}""";
            return auth_header;
        }
        #endregion

        #region Private Methods
        private void ValidateInputs(OAuth1Token tokeninfo, OAuth1TokenParam tokenParam) {
            //To generate a signature, irrespective of whether we send the secret across or not, we need, ConsumerKey and Consumer secret.
            if (tokeninfo == null) throw new ArgumentNullException(nameof(OAuth1Token));
            if (tokeninfo.Secret == null) throw new ArgumentNullException(nameof(OAuth1Token.Secret));
            if (string.IsNullOrWhiteSpace(tokeninfo.Secret.ConsumerKey)) throw new ArgumentNullException(nameof(OAuth1Token.Secret.ConsumerKey));
            if (string.IsNullOrWhiteSpace(tokeninfo.Secret.ConsumerSecret)) throw new ArgumentNullException(nameof(OAuth1Token.Secret.ConsumerSecret));
            if (string.IsNullOrWhiteSpace(tokenParam?.RequestURL?.ToString())) throw new ArgumentNullException(nameof(OAuth1TokenParam.RequestURL));

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
            var byteArray = Encoding.UTF8.GetBytes(input);
            //var byteArray = Encoding.ASCII.GetBytes(input);
            var hash = algorithm.ComputeHash(byteArray); //algo will already contain the key required.
            return Convert.ToBase64String(hash);
        }

        private string GenerateSignature(OAuth1Token tokeninfo,string base_string) {
            string result = String.Empty;

            //Check : https://developer.twitter.com/en/docs/authentication/oauth-1-0a/creating-a-signature

            //Prepare
            string cons_secret = Uri.EscapeDataString(tokeninfo.Secret.ConsumerSecret);
            string token_secret = "";
            if (tokeninfo.Secret != null && !string.IsNullOrWhiteSpace(tokeninfo.Secret.TokenSecret)) {
                token_secret = Uri.EscapeDataString(tokeninfo.Secret.TokenSecret); //Cannot escape null value so change to empty.
            }
            var _key = string.Concat(cons_secret, "&", token_secret); //Use both secrets for signing

            //Note that there are some flows, such as when obtaining a request token, where the token secret is not yet known. In this case, the signing key should consist of the percent encoded consumer secret followed by an ampersand character ‘&’.

            //Encrypt
            switch (tokeninfo.SignatureType) {
                case SignatureType.HMACSHA1:
                    var hmac = new HMACSHA1();
                    hmac.Key = Encoding.UTF8.GetBytes(_key); //Key as a byte array
                    //hmac.Key = Encoding.ASCII.GetBytes(_key); //Key as a byte array
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
            return result;
        }

        private string GenerateBaseString(RestParamList paramList, OAuth1Token tokeninfo,OAuth1TokenParam tokenparam) {
            //Concatenate all the header params to generate the base string that will be signed in next steps.

            //1.HTTP Method name followed by ampersand
            StringBuilder sb = new StringBuilder(tokenparam.Method.ToString().ToUpper()); //Get the HTTP Method name
            sb.Append("&");

            //2. Reqeust URL (percent encoded) followed by ampersand
            //sb.Append(NetUtils.UrlEncodeRelaxed(NetUtils.OAuthUtils.ConstructRequestUrl(tokenparam.RequestURL))); //can also directly give the string.
            sb.Append(Uri.EscapeDataString(NetUtils.OAuth.ConstructRequestUrl(tokenparam.RequestURL))); //can also directly give the string.
            sb.Append("&");

            //3. Append other parameters alphabetically. (Ensure all values have some value)
            //sb.Append(NetUtils.UrlEncodeRelaxed(paramList.GetConcatenatedString(encodevalues:true))); //Get concated string of the params (to do : url encoding)
            //the values of the params should be encoded as well. (values will be encoded first, then the whole generate string will be encoded)
            sb.Append(Uri.EscapeDataString(paramList.GetConcatenatedString(encodevalues:true))); //Get concated string of the params (to do : url encoding)
            return sb.ToString();
        }

        private Dictionary<string, string> GenerateHeaderParams(OAuth1Token tokeninfo, OAuth1TokenParam tokenparam) {

            //As specified in Section 3.1 of RFC5849 
            var unixtimestamp = NetUtils.OAuth.GetTimeStamp();
            var nonce = NetUtils.OAuth.GetNonce();

            //SIGNATURE SHOULD NOT BE GENERATED HERE.
            SortedDictionary<string, string> result = new SortedDictionary<string, string>(); //Sorted information is mandatory
            //Mandatory

            result.Add(RestConstants.OAuth.ConsumerKey, tokeninfo.Secret.ConsumerKey);
            result.Add(RestConstants.OAuth.Nonce, nonce);
            result.Add(RestConstants.OAuth.TimeStamp, unixtimestamp);
            result.Add(RestConstants.OAuth.SignatureMethod, tokeninfo.SignatureType.GetDescription());
            result.Add(RestConstants.OAuth.Version, tokeninfo.Version);

            //Optional
            if (!string.IsNullOrWhiteSpace(tokenparam.Verifier)) {
                result.Add(RestConstants.OAuth.Verifier, tokenparam.Verifier);
            }

            if (!string.IsNullOrWhiteSpace(tokenparam.CallBackURL?.ToString())) {
                result.Add(RestConstants.OAuth.Callback, tokenparam.CallBackURL.ToString());
            }

            if (!string.IsNullOrWhiteSpace(tokenparam.SessionHandle)) {
                result.Add(RestConstants.OAuth.SessionHandle, tokenparam.SessionHandle);
            }

            //Type based

            switch (tokenparam.RequestType) {
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
