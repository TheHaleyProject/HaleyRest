using System;
using System.Diagnostics;
using Haley.Utils;
using Haley.Models;
using System.Threading.Tasks;

namespace HaleyRest.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World");
            prepareClients();
            apiTest();
        }

        static void prepareClients()
        {
            ClientStore.AddClient(clientNames.jsonplaceHolder, new MicroClient(@"https://jsonplaceholder.typicode.com"));
            ClientStore.AddClient(clientNames.gorest, new MicroClient(@"https://gorest.co.in/"));
            ClientStore.AddClient(clientNames.publicAPI, new MicroClient(@"https://api.publicapis.org/"));
        }

        static async void apiTest()
        {
            var _response = ClientStore.GetClient(clientNames.publicAPI).InvokeAsync("entries", null).Result;

        }
    }
    public enum clientNames
    {
        jsonplaceHolder,
        gorest,
        publicAPI,
    }
}
