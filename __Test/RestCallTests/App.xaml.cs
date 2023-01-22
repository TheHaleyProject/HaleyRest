using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.Rest;
using Haley.Utils;
using System.Text;
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
using System.Threading;
using TinyOAuth1;
using System.Net.Http;
namespace RestCallTests
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e) {
            var title = AssemblyUtils.GetInfo(AssemblyInfo.Title);
            var des1 = AssemblyUtils.GetInfo(AssemblyInfo.Description);
            var title2 = AssemblyUtils.GetInfo(AssemblyInfo.Product);
            var title3 = AssemblyUtils.GetInfo(AssemblyInfo.Version);
            var title4 = AssemblyUtils.GetInfo(AssemblyInfo.Company);
            var title5 = AssemblyUtils.GetInfo(AssemblyInfo.Trademark);
            var title6 = AssemblyUtils.GetInfo(AssemblyInfo.Copyright);

            RestCall();

            ClientStore.AddClient("betaclient", new FluentClient($@""));
            var window = new MainWindow();
            window.Show();
            //Task.Run(() => APIService.InitiateSelfHostNetCore());
        }

        private void Application_Exit(object sender, ExitEventArgs e) {

        }

        private void RestCall() {
            //var _tinyOAuth = new TinyOAuth(new TinyOAuthConfig() {ConsumerKey="4579bfc5-0671-4087-bed3-00a41b5cff8c",ConsumerSecret= "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5" });
            ////var _accessTokenSecret = accessTokenSecret;
            ////var _accessToken = accessToken;
            //var request1 = new HttpRequestMessage();
            //request.Headers.Authorization = _tinyOAuth.GetAuthorizationHeader(_accessToken, _accessTokenSecret,
            //    request.RequestUri.AbsoluteUri, request.Method);

            //return base.SendAsync(request, cancellationToken);
        }
    }
}
