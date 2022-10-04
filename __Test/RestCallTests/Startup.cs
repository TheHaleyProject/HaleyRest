using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.Rest;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Net;
using System.Net.Http;

#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Owin;
    using System.Web;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
#elif NET472_OR_GREATER
using System.Web.Http;
    using Owin;
#endif

namespace RestCallTests
{
    public class Startup
    {

#if NETCOREAPP3_1_OR_GREATER || NET5_0_OR_GREATER
         public void ConfigureServices(IServiceCollection services) {
                    services.AddControllers();
                }

                public void Configure(IApplicationBuilder app) {
                    app.UseRouting();
                    app.UseEndpoints(epoint => { epoint.MapControllers(); });
                    app.UseOwin(); //Not mandatory
                }
#elif NET472_OR_GREATER
        public void Configuration(IAppBuilder appBuilder) {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            appBuilder.UseWebApi(config);
            //appBuilder.Run(sam => {
            //    sam.Response.ContentType = "text/plain";
            //    return sam.Response.WriteAsync("Service started.");
            //});
        }
#endif
    }
}
