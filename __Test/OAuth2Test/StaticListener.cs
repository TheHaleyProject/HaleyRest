using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2Test
{
    public static class GlobalHelper
    {
        static bool _listenerInititated = false;
        static IWebHost _host;
        static string _listenerPort = "9600";
        static string _listenerURL = null;
        static ConcurrentDictionary<string, List<Action<object>>> _listenerCallbacks = new ConcurrentDictionary<string, List<Action<object>>>();

        public static void SendMessage(string key, object message) {
            if (_listenerCallbacks.TryGetValue(key,out var listeners)) {
                foreach (var listener in listeners) {
                    try {
                        listener.Invoke(message);
                    } catch (Exception) {
                        continue;
                    }
                }
            }
        }

        public static void RegisterCallBack(string key, Action<object> callback) {

            if (!_listenerCallbacks.ContainsKey(key)) _listenerCallbacks.TryAdd(key, new List<Action<object>>());
            _listenerCallbacks[key].Add(callback);
        }

        public static void SelfHostListener() {
            //the listener or the web application blocks the calling thread until shutdown. So call it inside a separate thread.
            try {
                if (_listenerInititated) {
                    return;
                }

                _listenerInititated = true;
                _host = new WebHostBuilder()
                                .UseKestrel()
                                .UseUrls(GetListenerURL())
                                .UseStartup<ListenerStartup>()
                                .Build();

                Task.Run(() => { _host.Run(); });  //Run the host and block this thread. //We can also call _host?.StopAsync() to stop it if needed
            } catch (Exception ex) {
                
            }
        }

        static string GetListenerURL() {
            if (_listenerURL == null) {
                var port = string.IsNullOrWhiteSpace(_listenerPort) ? "9600" : _listenerPort;
                _listenerURL = $@"http://localhost:{port}";
            }
            return _listenerURL;
        }

    }

    internal class ListenerStartup {
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app) {
            app.UseRouting(); 
            app.UseEndpoints(epoint => { epoint.MapControllers(); });
            //app.UseOwin(); //Not mandatory
        }
    }
}
