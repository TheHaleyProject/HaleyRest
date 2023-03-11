using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.MVVM;
using Haley.Models;
using System.Windows.Input;
using System.Diagnostics;
using Haley.Rest;
using Haley.Abstractions;
using Haley.Utils;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows.Controls;
using System.Windows;
using System.Text.Json;
using System.Text.Json.Nodes;
using OAuth2Test.Models;
using Haley.Services;
using Microsoft.AspNetCore.Components;

namespace OAuth2Test
{
    public class AuthVM : BaseVM
    {
        GAuthToken _token = new GAuthToken();
        const string gAuthURL = @"https://accounts.google.com/o/oauth2/v2/auth";
        const string gExchangeURL = @"https://oauth2.googleapis.com/token";
        DialogService _ds = new DialogService();

        BrowserWindow bw;

        public ICommand CmdGoogleLogin => new DelegateCommand(googleLogin);

        private async void googleLogin() {
            await googleLoginAsync();
        }

        async Task googleLoginAsync() {
            Debug.WriteLine("We are here.");

            var querylist = new List<QueryParam>();
            querylist.Add(new QueryParam("response_type", "code"));
            querylist.Add(new QueryParam("scope", @"https://www.googleapis.com/auth/drive.file"));
            querylist.Add(new QueryParam("client_id", _token.ClientId));
            querylist.Add(new QueryParam("redirect_uri", @"http://localhost:9600/api/GAuth/authorised")); //This is dummy at present
            querylist.Add(new QueryParam("hl", @"en"));
            var qlist = new QueryParamList(querylist);
            var query = gAuthURL + "?" + qlist.GetConcatenatedString();

            bw = new BrowserWindow(query,false);
            bw.ShowDialog();

            //var client = GetClient();
            //var res = await client
            //    .WithEndPoint(gAuthURL)
            //    .WithQueries(querylist)
            //    .GetAsync();
            //var resStr = await res.AsStringResponseAsync();
            //if (resStr.IsSuccessStatusCode) {
            //    ////Debug.Write(resStr.Content);
            //    ////Get a temp location and store this html file
            //    //var htmlPath = Path.GetTempFileName() + ".html";
            //    //File.WriteAllText(htmlPath, resStr.Content);
            //    //Process.Start(new ProcessStartInfo { FileName = htmlPath, UseShellExecute = true });
            //    ////Process.Start(htmlPath); //This will open the html page in default browser.
            //    BrowserWindow bw = new BrowserWindow(resStr.Content);
            //    bw.ShowDialog();
            //}
        }

        async Task Initialize() {
            _token.ClientId = "529075142760-eu3uvtcuojl0kk296sfjnb4gepfl5mgi.apps.googleusercontent.com";
            _token.ClientSecret = "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";

            GlobalHelper.SelfHostListener(); //Do not await
            await Task.Delay(1500); //Wait for 1.5 seconds.\
            GlobalHelper.RegisterCallBack("gAuthorised", HandleGAuthorised);
        }

        private async void HandleGAuthorised(object obj) {
            if (!(obj is string code)) return;
            //get this code and conver it to Access token.
            bw.Dispatcher.Invoke(() => {
                bw.CloseWindow(); //Close window from GUI thread.
            });
            var querylist = new List<QueryParam>();
            querylist.Add(new QueryParam("grant_type", "authorization_code"));
            querylist.Add(new QueryParam("code", code));
            querylist.Add(new QueryParam("client_id", _token.ClientId));
            querylist.Add(new QueryParam("client_secret", _token.ClientSecret));
            querylist.Add(new QueryParam("redirect_uri", @"http://localhost:9600/api/GAuth/authorised")); //This is dummy at present

            var client = GetClient();
            var res = await client
                .WithEndPoint(gExchangeURL)
                .WithParameter(new FormEncodedRequest(querylist))
                .PostAsync();
            var resStr = await res.AsStringResponseAsync();
            if (resStr.IsSuccessStatusCode) {
                var jobj = JsonObject.Parse(resStr.Content);
                _token.AccessToken = jobj["access_token"]?.ToString();
                _token.RefreshToken = jobj["refresh_token"]?.ToString();
                _token.ExpiresIn = jobj["expires_in"]?.ToString();
                if (!string.IsNullOrWhiteSpace(_token.ExpiresIn)) {
                    _token.ExpiresAt = DateTime.UtcNow.AddSeconds(double.Parse(_token.ExpiresIn)); //UTC expiration time.
                }
                var tokenProvider = new TokenAuthProvider();
                tokenProvider.SetToken(_token.AccessToken);
                GetClient().SetAuthenticator(tokenProvider);

                var userDet = await ( await GetClient()
                    .WithEndPoint("https://www.googleapis.com/drive/v3/about")
                    .WithQuery(new QueryParam("fields", "user"))
                    .GetAsync()).AsStringResponseAsync();
                if (userDet.IsSuccessStatusCode) {

                    Application.Current.Dispatcher.Invoke(() => {
                        _ds.SendToast("User logged", userDet.Content, Haley.Enums.NotificationIcon.Success);
                    });
                }
            }
        }

        IClient GetClient() {

            if(ClientStore.GetClient("gAuth") == null) {
                ClientStore.AddClient("gAuth", new FluentClient());
            }
            return ClientStore.GetClient("gAuth");
        }

        public AuthVM() {
            Initialize();
        }
    }
}
