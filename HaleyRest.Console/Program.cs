using System;
using System.Diagnostics;
using Haley.Utils;
using Haley.Models;
using Haley.Rest;
using System.Threading.Tasks;
using Haley.Abstractions;
using Haley.Enums;
using System.Threading;

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

            Thread t1 = new Thread(new ThreadStart(()=> {_res1 =  apiTest(clientNames.publicAPI, "entries").Result; }));
            Thread t2 = new Thread(new ThreadStart(() => { _res2 = apiTest(clientNames.gorest, "/public/v1/users/123/posts").Result; }));
            Thread t3 = new Thread(new ThreadStart(() => { _res3 = apiTest2(clientNames.publicAPI, "entries").Result; }));
            Thread t4 = new Thread(new ThreadStart(() => { _res4 = apiTest(clientNames.publicAPI, "entries").Result; }));

            t1.Start();
            t2.Start();
            t3.Start();
            t4.Start();
            t1.Join(); //Wait until t1 completes.
            t2.Join();
            t3.Join();
            t4.Join();
            
            if (_res1.IsSuccess)
            {
                Console.WriteLine("Res 1 Success");
            }
        }

        static async Task<IResponse> apiTest(clientNames name,string url)
        {
            var _client = ClientStore.GetClient(name);
            try
            {
                Console.WriteLine($@"Calling {name}");
                var _response = await _client.BlockClient().GetAsync(url);
                return _response;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                _client.UnBlockClient();
            }
        }
        static async Task<IResponse> apiTest2(clientNames name, string url)
        {
            var _client = ClientStore.GetClient(name);
            try
            {
                Console.WriteLine($@"Calling {name}");
                var _response = await _client.UnBlockClient().GetAsync(url);
                return _response;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                _client.UnBlockClient();
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
