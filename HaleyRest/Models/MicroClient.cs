using Haley.Enums;
using Haley.Utils;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;

namespace Haley.Models
{
    /// <summary>
    /// A simple straightforward HTTP helper client.
    /// </summary>
    public sealed class MicroClient
    {
        public HttpClient BaseClient { get; }
        private Uri _base_uri;
        private string jwt_token;

        public void ResetHeaders()
        {
            //remains the same throught the life time of this client.
            BaseClient.BaseAddress = _base_uri;
            BaseClient.DefaultRequestHeaders.Accept.Clear();
            BaseClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            //BaseClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
        }

        /// <summary>
        /// This authentication will NOT be added to the headers,as the client is re-used. This will be added to each request header (if authorization is requested).
        /// </summary>
        /// <param name="token"></param>
        /// <param name="token_prefix"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public MicroClient SetDefaultRequestAuthenication(string token, string token_prefix = "Bearer")
        {
            jwt_token = token_prefix ?? "";
            jwt_token = jwt_token + " " + token;
            jwt_token?.Trim();
            return this;
        }

        public MicroClient SetDefaultHeaderAuthentication(string token, string token_prefix = "Bearer")
        {
            SetDefaultRequestAuthenication(token, token_prefix);
            ResetHeaders(); //Re initiate the client (clearing old headers)
            if (!string.IsNullOrWhiteSpace(jwt_token))
            {
                //If it is null, then do not set anything. However, it would have already been cleared.
                BaseClient.DefaultRequestHeaders.Add("Authorization", jwt_token);
                BaseClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt_token);
            }
            return this;
        }

        public MicroClient(string base_address) 
        {
            _base_uri = getBaseUri(base_address);
            if (_base_uri == null)
            {
                Debug.WriteLine($@"ERROR: Base URI is null. MicroClient cannot be created.");
                return;
            }
            BaseClient = new HttpClient(); //Base client is read only. So initiate only once.
            ResetHeaders();
        }

        public MicroClient(Uri base_uri):this(base_uri.AbsoluteUri) { }

        #region Helpers

        //public async Task<HttpResponseMessage> InvokeAsync(object content, string url, Method rest_method = Method.Get, bool request_as_query = false, bool is_anonymous = false, ReturnFormat return_format = ReturnFormat.Json, bool serializeObject = true)
        //{
        //    try
        //    {
        //        Dictionary<DataType, (string name,object value)> request_content = new Dictionary<DataType, (string name, object value)>();
        //        DataType request_method = DataType.RequestBody;
        //        if (request_as_query) request_method = DataType.QueryString;
        //        request_content.Add(request_method, content);
        //        return await InvokeAsync(request_content, url, rest_method, is_anonymous, return_format, serializeObject);
        //    }
        //    catch (Exception ex)
        //    {
        //        return new HttpResponseMessage(HttpStatusCode.ExpectationFailed) {ReasonPhrase = ex.ToString() };
        //    }
        //}


        //public async Task<HttpResponseMessage> InvokeAsync(Dictionary<DataType, object> content, string url, Method rest_method = Method.Get, bool authorize = false, ReturnFormat return_format = ReturnFormat.Json, bool serializeObject = true)
        //{
        //    HttpRequestMessage _request = new HttpRequestMessage();
        //    _request.Content
        //    //ADD HEADERS
        //    if (authorize) //Need authorization to proceed. If authorization is not available, throw error
        //    {
        //        if (string.IsNullOrEmpty(_token)) throw new ArgumentException("Authorization String not found"); //Remember you need to have a jwt value in session for processing.

        //        _request.AddHeader("Authorization", _jwt); //If token prefix is null, then send in nothing.
        //    }

        //    if (content == null)
        //    {
        //        Debug.WriteLine("Content is empty. Nothing to process.");
        //    }
        //    DataType request_type = DataType.QueryString;
        //    foreach (var item in content)
        //    {
        //        request_type = rest_method == Enums.Method.Get ? DataType.QueryString : item.Key;  //For a Get method, it should only be sent as query because get methods cannot have a body.
        //        _fillRequestParameters(item.Value, _request, request_type, serializeObject); //Sometimes, we just send in a dicitionary. sometimes we send in a object.
        //    }

        //    try
        //    {
        //        //Below lines are required because sometimes it results in error during async methods.
        //        ServicePointManager.Expect100Continue = true;
        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
        public async Task<HttpResponseMessage> InvokeAsync(string url, object content, Method method = Method.Get)
        {
            //Now this object can be serialized and sent inisde the httpcontent.
            //if this is Post or Put, we send as content.
            var json = JsonSerializer.Serialize(content); //What if the object doesn't need to be serialized.? what if it was already serialized?
            var _content =  new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await InvokeAsync(url, _content, method);
        }


        public async Task<HttpResponseMessage> InvokeAsync(string url, HttpContent content,Method method = Method.Get)
        {
            HttpMethod request_method = HttpMethod.Get;
            switch (method)
            {
                case Method.Get:
                    return await BaseClient.GetAsync(url);
                case Method.Post:
                    request_method = HttpMethod.Post;
                    break;
                case Method.Delete:
                    request_method = HttpMethod.Delete;
                    break;
                case Method.Update:
                    request_method = HttpMethod.Put;
                    break;
            }
            var request = new HttpRequestMessage(request_method, parseURI(url).resource_part) {Content = content }; //URL should not have base part.

            return await InvokeAsync(request);
        }

        public async Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request)
        {
            return await BaseClient.SendAsync(request);
        }

        private (string base_part, string resource_part) parseURI(string input_url)
        {
            try
            {
                if (string.IsNullOrEmpty(input_url)) return (null, null);
                if(Uri.TryCreate(input_url,UriKind.RelativeOrAbsolute,out Uri _uri))
                {
                    if (_uri.IsAbsoluteUri)
                    {
                        string _base = _uri.GetLeftPart(UriPartial.Authority);
                        string _method = input_url.Substring(_base.Length);
                        return (_base, _method);
                    }
                }
                return (null, input_url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: " + ex.StackTrace);
                return (null, input_url);
            }
        }
        private (string base_part, string resource_part) parseURI(Uri input_uri)
        {
            return parseURI(input_uri.AbsoluteUri);
        }
        private Uri getBaseUri(string address)
        {
            bool result = Uri.TryCreate(address, UriKind.Absolute, out var uri_result)
                && (uri_result.Scheme == Uri.UriSchemeHttp || uri_result.Scheme == Uri.UriSchemeHttps);
            if (result) return uri_result;
            Console.WriteLine($@"ERROR: Unable to create URI from the address {address}");
            return null;
        }
        private Uri getBaseUri(Uri inputURI)
        {
            return getBaseUri(inputURI.AbsoluteUri);
        }
        #endregion
    }
}
