using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;

namespace RestCallTests
{
    public static class APIService
    {
        private static bool listener_initiated = false;
        public static string port = "9780";
        public static bool InitiateSelfHostNetCore(out string message) {
            try {
                message = "API Listener Successfully initiated.";
                if (listener_initiated) return true;
                var host = new WebHostBuilder()
              .UseKestrel()
              .UseUrls($@"http://*:{port?? "9780"}")
              .UseStartup<Startup>()
              .Build();

                host.Run();
                return true;
            }
            catch (Exception ex) {
                message = "Failed to initiate API listener.";
                return false;
            }
        }
    }
}
