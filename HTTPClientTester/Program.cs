using System;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Diagnostics;
using Haley.Rest;
using Haley.Models;
using Haley.Abstractions;
using Haley.Utils;
using System.Collections.Generic;

namespace HTTPClientTester
{
    class Program
    {
        static Stopwatch swatchMain = new Stopwatch();
        static Stopwatch swatch = new Stopwatch();
        static void Main(string[] args)
        {
            //www.test.com/posts/
            //CRUD - get, put, post, delete,
            //https://datausa.io/api/data?drilldowns=Nation&measures=Population
            //https://jsonplaceholder.typicode.com/posts/1


            //AUTHORITY / BASE URI -
            //https://jsonplaceholder.typicode.com/
            //https://datausa.io/
            //https://gorest.co.in/public/v1/users

            //ENDPOINT -
            // posts 
            // api/data
            //PARAMTERS OR REQUEST BODY. - 1 (id =1), ?drilldowns=Nation&measures=Population

            //11 seconds. disposing method.
            swatchMain.Start();
            ClientStore.AddClient("gorest", @"https://gorest.co.in/");
            ClientStore.AddClient("datausa", @"https://datausa.io");
            ClientStore.AddClient("jph", @"https://jsonplaceholder.typicode.com/");

            var _userendpoint = @"public/v1/users";
            var _postendpoint = @"public/v1/posts";
            //{"id":72,"name":"kumar katkuri","email":"kk1@gmail.com","gender":"male","status":"active"}

            Dictionary<string, string> _input = new Dictionary<string, string>();
            _input.Add("name", "Lingam");
            _input.Add("email", "logtester@test.com");
            _input.Add("gender", "male");
            _input.Add("status", "active");

            Dictionary<string, string> _input2 = new Dictionary<string, string>();
            _input2.Add("id", "2983");

            //Call("gorest", _endpoint);
            //Call("gorest", _endpoint,_input,true);
            //Call("gorest", _userendpoint, _input2);

            Call("gorest", _userendpoint, _input2);

            //Call("posts", _testContent, true);
            //Call("public/v1/users", _testContent, true);
            swatchMain.Stop();
            Console.WriteLine($@"Total consumed time is {swatchMain.Elapsed.TotalSeconds.ToString()} seconds.");
        }

        static void Call(string clientKey, string endpoint,object content = null, bool isPost = false)
        {
            swatch.Start();
            var _client = ClientStore.GetClient(clientKey);
            if (_client == null) return;
            IResponse _res = null;
            //Use new http every call or reuse httpclient.
            if (!isPost)
            {
                if (content.IsDictionary())
                {
                
                    _res = _client.GetByDictionaryAsync(endpoint, content as Dictionary<string, string>).Result;
                }
                else
                {
                    _res = _client.GetAsync(endpoint).Result;
                }
            }
            else
            {
                _client.ClearRequestAuthentication();
                _client.AddRequestAuthentication("454a383cafde0aa62920e096aaa0f979e79c50e50e108845b8aac8d35bf68db0");
                if (content is string)
                {
                    //HTTP CONtent. StringContent, Stream Content, Bytearrayconten, multiform data content.. 
                    _res = _client.PostObjectAsync(endpoint, content, true).Result;
                }
                else if (content.IsDictionary())
                {
                    _res = _client.PostDictionaryAsync(endpoint, content as Dictionary<string,string>).Result;
                }
            }
            string contentmsg = string.Empty;

            if (_res == null)
            {
                Console.WriteLine("Response is null");
                return;
            }
            if (_res is StringResponse strRsp)
            {
                contentmsg = strRsp.StringContent;
            }
                var _resStatus = _res.IsSuccessStatusCode ? "Success" : "Failed";
                swatch.Stop();
                var _tspan =swatch.Elapsed;
                swatch.Reset();
            //Console.WriteLine($@"{_resStatus} - {_res.ReasonPhrase} - {_contentStr}");
            Console.WriteLine($@"{_resStatus} - {_res} - {_tspan.TotalSeconds.ToString()} - request uri - {_res.OriginalResponse.RequestMessage.RequestUri} {Environment.NewLine} {contentmsg}");
        }
    }
}
