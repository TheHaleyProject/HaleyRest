using System;
using System.Diagnostics;
using Haley.Utils;
using Haley.Models;
using Haley.Rest;
using System.Threading.Tasks;

namespace HaleyRest.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World");
            prepareClients();
            apiTest(clientNames.publicAPI, "entries");
            apiTest(clientNames.gorest, "/public/v1/users/123/posts");
        }

        static void prepareClients()
        {
            ClientStore.AddClient(clientNames.jsonplaceHolder, @"https://jsonplaceholder.typicode.com");
            ClientStore.AddClient(clientNames.gorest, @"https://gorest.co.in/");
            ClientStore.AddClient(clientNames.publicAPI, @"https://api.publicapis.org");
        }

        static void apiTest(clientNames name,string url)
        {
            var _response = ClientStore.GetClient(name).GetAsync(url, null).Result;
        }
    }
    public enum clientNames
    {
        jsonplaceHolder,
        gorest,
        publicAPI,
    }
}
