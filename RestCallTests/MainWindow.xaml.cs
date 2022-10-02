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

namespace RestCallTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        
        public MainWindow() {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e) {
            string msg = string.Empty;
            APIService.port = string.IsNullOrWhiteSpace(tbxPort.Text) ? "9780" : tbxPort.Text;
            tblock_msg.Text = "Initiating listener";
            if (await Task.Run(() => APIService.InitiateSelfHostNetCore(out msg))) {
                tblock_msg.Text = msg;
                await LogInCall();
            }
            else {
                tblock_msg.Text = msg;
            }
        }

        private async Task<bool> LogInCall() {
            try {
                var client = new FluentClient($@"https://daep.withbc.com");
                var _res3 = await client.SendAsync(Haley.Enums.Method.POST);
                var _res2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/"));

                client.SetAuthenticator(new OAuth1Authenticator("4579bfc5-0671-4087-bed3-00a41b5cff8c", "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5")); //Set for all 
                //client.SetAuthenticator(new Haley.Utils.TokenAuthenticator().SetToken(_header,"OAuth")); //Set for all 
                var _res4 = await client
                            .WithEndPoint($@"oauth/request_token")
                            .InheritAuthentication() //Inherit from parent
                            .SetAuthParam(new OAuth1TokenParam() 
                                    { RequestType = OAuthRequestType.RequestToken,
                                     CallBackURL = new Uri($@"http://localhost:{APIService.port}/api/callback/authorised")})
                            .SendAsync(Method.POST);


                var result = await _res4.AsStringResponse();
                if (!string.IsNullOrWhiteSpace(result?.Content)) {
                    var _dic = NetUtils.OAuth.ParseQueryParameters(result.Content, null);
                    var oauth_token = _dic[RestConstants.OAuth.Token];
                    var temp_oauth_token_secret = _dic[RestConstants.OAuth.TokenSecret];
                    //Now authorise the token
                    var authorise_url = $@"{client.URL}/oauth/authorise?{RestConstants.OAuth.Token}={oauth_token}";
                    var ps = new ProcessStartInfo(authorise_url)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    Process.Start(ps);
                }
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
