using System;
using System.Diagnostics;
using Haley.Utils;
using Haley.Models;
using Haley.Rest;
using System.Threading.Tasks;
using Haley.Abstractions;
using Haley.Enums;
using System.Threading;
using System.Collections.Generic;

namespace HaleyRest.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World");
            prepareClients();
            Test().Wait();
        }

        static void prepareClients()
        {
            ClientStore.AddClient(clientNames.jsonplaceHolder, @"https://jsonplaceholder.typicode.com");
            ClientStore.AddClient(clientNames.gorest, @"https://gorest.co.in/");
            ClientStore.AddClient(clientNames.publicAPI, @"https://api.publicapis.org");
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
                var _response = await _client.BlockClient(message).GetAsync(url);
                return _response;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                _client.UnBlockClient(message);
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
                var _response = await _client.BlockClient(5, message).GetByParamsAsync(url,_params.ToRequestParams(true));
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
                var _response = await _client.BlockClient(6, message).SendObjectAsync(url,new QueryParam("auth","null",true),Method.GET);
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
