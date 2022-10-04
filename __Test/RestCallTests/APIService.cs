using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Threading.Tasks;

#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Owin;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
#elif NET472_OR_GREATER
using System.Web.Http;
using System.Net.Http;
using Microsoft.Owin.Hosting;
#endif

namespace RestCallTests
{
    public static class APIService
    {
        private static bool firstInitiation = true;
        public static string port = "9780";
        public static string message = string.Empty;


        public static void InitiateSelfHostNetCore() {
            try {
                if (!firstInitiation) { return; }
                firstInitiation = false;

#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER

             var host = new WebHostBuilder()
                          .UseKestrel()
                          .UseUrls($@"http://*:{port ?? "9780"}")
                          //.UseUrls("http://*:9780")
                          .UseStartup<Startup>()
                          .Build();
                           host.Run(); //Runs the web application and blocks the calling thread until shutdown.

#elif NET472_OR_GREATER

                WebApp.Start<Startup>(url: $@"http://localhost:{port ?? "9780"}");
#endif
            } catch (Exception ex) {
                message = "Failed to initiate API listener.";
            }
        }
    }
}
