using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using Haley.Rest;
using Haley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
//using RestSharp;
//using RestSharp.Authenticators;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;
using System.Security.Cryptography;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args) {
            Console.WriteLine("Hello, World!");
            //URITesting();
            var res1 = CallMethod().Result;
            //var res2 = RestSharpCall().Result;
            //var resfinal = TinyCall().Result;
        }

        static void URITesting() {
            //string test = $@"23452352@#$%^&*@!%!&";
            //Console.WriteLine($@"Actual - {test}");
            //var URIEscapted = Uri.EscapeUriString(test);
            //var DataEscaped = Uri.EscapeDataString(test);
            //Console.WriteLine($@"URI Escaped - {URIEscapted}");
            //Console.WriteLine($@"DATA Escaped - {DataEscaped}");
            //Console.WriteLine($@"URI DOUBLE Escaped - {Uri.EscapeUriString(URIEscapted)}");
            //Console.WriteLine($@"DATA DOUBLE Escaped - {Uri.EscapeDataString(DataEscaped)}");
        }
        static async Task<bool> CallMethod() {
            try {
                var client = new FluentClient($@"https://daep.withbc.com");
                var _res3 = await client.SendAsync(Haley.Enums.Method.POST);
                var _res2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/"));

                client.SetAuthenticator(new OAuth1Authenticator("4579bfc5-0671-4087-bed3-00a41b5cff8c", "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5")); //Set for all 
                //client.SetAuthenticator(new Haley.Utils.TokenAuthenticator().SetToken(_header,"OAuth")); //Set for all 
                var _res4 = await client.WithEndPoint($@"oauth/request_token").InheritAuthentication().SetAuthParam(new OAuth1TokenParam() { RequestType = OAuthRequestType.RequestToken }).SendAsync(Method.POST);
                var result = await _res4.AsStringResponse();
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return false;
            }

        }
    }
}