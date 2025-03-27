using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Rest;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HaleyRest.ConsoleTest
{
    class Program
    {
        [STAThread]
        static  void Main(string[] args)
        {
            //Console.WriteLine( CDETest().Result);
            var res = GoogleTest().Result;
            //var str = "https://jsonplaceholder.typicode.com";
            //var str2 = @"%23helloworld12342!#P(*!U!@))#@)$!)!~$@514988!@3";
            //Console.WriteLine(NetUtils.URLSingleEncode(str));

            //Console.WriteLine(NetUtils.URLSingleEncode(str2));
            //Console.WriteLine(NetUtils.URLSingleEncode(Uri.EscapeUriString(str2),str2));
            //Console.WriteLine("Hello World");
            //prepareClients();
            //Test().Wait();
        }

        async static Task<bool> GoogleTest() {
            var client = new FluentClient();
            var result = await client
                .WithEndPoint("https://accounts.google.com/o/oauth2/v2/auth")
                .WithQueries(new List<QueryParam>() {
                    new QueryParam("client_id",""),
                    new QueryParam("redirect_uri","http://localhost:9500/api/authorise"),
                    new QueryParam("response_type","code"),
                    new QueryParam("scope","https://www.googleapis.com/auth/drive.appdata https://www.googleapis.com/auth/drive.file")})
                .GetAsync();
            var resultstr = await result.AsStringResponseAsync();
            if (resultstr.IsSuccessStatusCode) {

            }
            return true;
        }

        async static Task<bool> CDETest() {
            var client = new FluentClient(@"");
            client.SetAuthenticator(new OAuth1Provider("4579bfc5-0671-4087-bed3-00a41b5cff8c", "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5"));

            //Get Request Token
            var _res = await (await client
                .WithEndPoint(@"api/services/simple_search?klang=%28t%3Ashare%29%3B%28creatingcompanyname%3Aasg%2A%29&page=2")
                .SetAuthParam(new OAuth1RequestInfo(new OAuthToken("a4f7f643-4626-4d22-9986-e29a891e5600", "e95d-5faf-d033-36f9-a38b-50c4-b0bc-4e77"), OAuthRequestType.AccessToken))
                .GetAsync()).AsStringResponseAsync();

            return true;
        }

        static void prepareClients()
        {
            ClientStore.AddClient(clientNames.jsonplaceHolder, new FluentClient(@"https://jsonplaceholder.typicode.com"));
            ClientStore.AddClient(clientNames.gorest, new FluentClient(@"https://gorest.co.in/"));
            ClientStore.AddClient(clientNames.publicAPI,  new FluentClient( @"https://api.publicapis.org"));
        }

        static async Task Test()
        {
            IResponse _res1 = null;
            IResponse _res2 = null;
            IResponse _res3 = null;
            IResponse _res4 = null;

            Thread t1 = new Thread(new ThreadStart(()=> {_res1 =  apiTest(clientNames.publicAPI, "entries","Thread 1").Result; }));
            Thread t2 = new Thread(new ThreadStart(() => { _res2 = apiTest(clientNames.gorest, "/public/v1/users/123/posts", "Thread 2").Result; }));
            Thread t3 = new Thread(new ThreadStart(() => { _res3 = apiTest2(clientNames.publicAPI, "entries", "Thread 3").Result; }));
            Thread t4 = new Thread(new ThreadStart(() => { _res4 = apiTest3(clientNames.publicAPI, "random", "Thread 4").Result; }));

            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            t1.Join(); //Wait until t1 completes.
            t2.Join();
            t3.Join();
            t4.Join();
            
            if (_res1.IsSuccessStatusCode)
            {
                Console.WriteLine("Res 1 Success");
            }
        }

        static async Task<IResponse> apiTest(clientNames name,string url,string message)
        {
            var _client = ClientStore.GetClient(name);
            try
            {
                Console.WriteLine($@"Calling {name}");
                var _response = await _client.WithEndPoint(url).GetAsync();
                return _response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        static async Task<IResponse> apiTest2(clientNames name, string url, string message)
        {
            var _client = ClientStore.GetClient(name);
            try
            {
                Dictionary<string, string> _params = new Dictionary<string, string>();
                _params.Add("category", "animals");
                Console.WriteLine($@"Calling {name}");
                var _response = await _client.WithEndPoint(url).WithParameters(_params.Select(p => new QueryParam(p.Key, p.Value))).GetAsync(); 
                return _response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        static async Task<IResponse> apiTest3(clientNames name, string url, string message)
        {
            var _client = ClientStore.GetClient(name);
            try
            {
                Console.WriteLine($@"Calling {name}");
                var _response = await _client.WithEndPoint(url).WithQuery(new QueryParam("auth", "null")).GetAsync(); 
                return _response;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
    public enum clientNames
    {
        jsonplaceHolder,
        gorest,
        publicAPI,
    }
}
