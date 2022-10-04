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
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
//using rs = RestSharp;
//using rsa = RestSharp.Authenticators;
//using RestSharp;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace RestCallTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private OAuth1Token GetNewToken() {
            return new OAuth1Token("4579bfc5-0671-4087-bed3-00a41b5cff8c", "f292-3177-e0b1-22ae-e253-5bfd-dbec-84d5");
        }
        private ConcurrentDictionary<string, (string secret,string verifier)> _temptokens = new ConcurrentDictionary<string, (string secret, string verifier)>();
        public MainWindow() {
            InitializeComponent();
            EventStore.Singleton.GetEvent<TokenAuthorisedEvent>().Subscribe(_tokenauthorisedhandler,option:InvokeOption.UIThread);
        }

        private void _tokenauthorisedhandler((string verifier, string token) obj) {
            try {
                var _mainToken = GetNewToken(); //gets a new token
                                                //A token is authorised, perform further tasks.
                                                //If this token is authorised
                if (string.IsNullOrWhiteSpace(obj.token) || string.IsNullOrWhiteSpace(obj.verifier)) return;
                //get secret 
                if (_temptokens.TryGetValue(obj.token, out var _tokensec)) {
                    _mainToken.Secret.UpdateTokenInfo(obj.token, _tokensec.secret);
                    //Ask for access token.
                    var client = ClientStore.GetClient("betaclient");
                    var response = client
                        .SetAuthenticator(new OAuth1Provider(_mainToken)) //Now, replace the authenticator with new updated values
                        .WithEndPoint($@"oauth/access_token")
                        .InheritAuthentication() //inherit for the request
                        .SetAuthParam(new OAuth1TokenParam() { Method = HttpMethod.Post, Verifier = obj.verifier, RequestType = OAuthRequestType.AccessToken })
                        .SendAsync(Haley.Enums.Method.POST)
                        .Result;
                    var responsestr = response.AsStringResponseAsync().Result;
                    if (!string.IsNullOrWhiteSpace(responsestr?.Content)) {
                        var _dic = NetUtils.OAuth.ParseQueryParameters(responsestr.Content, null);
                        var access_token = _dic[RestConstants.OAuth.Token];
                        var access_secret = _dic[RestConstants.OAuth.TokenSecret];
                        _mainToken.Secret.UpdateTokenInfo(access_token, access_secret);
                        client.SetAuthenticator(new OAuth1Provider(_mainToken)); //Set the token to the client
                        SetTblockMessage("Successfully connected with CDE.");
                        btnLogin.Dispatcher.Invoke(() => {
                            btnLogin.IsEnabled = false;
                            btnLogin.Visibility = Visibility.Collapsed;
                            btnGetUser.IsEnabled = true;
                            btnGetUser.Visibility = Visibility.Visible;
                        });
                    } else {
                        SetTblockMessage("Connection with CDE failed");
                    }
                }
            } catch (Exception ex) {
                throw;
            }
        }

        private void SetTblockMessage (string msg) {
            tblock_msg.Dispatcher.Invoke(() => {
                tblock_msg.Text = msg;
            });
        }

        private  async Task<bool> GetUserInfo() {
            try {
                var client = ClientStore.GetClient("betaclient");
                
                //Get API

                var _apipath = $@"api/";
                var root_json =await GetAPIResult(client, _apipath);
                var root = JsonNode.Parse(root_json);
                if (root == null) return false;

                //User
                var user_endpoint = root["current_state"]?["user"]?.GetValue<string>();
                if (user_endpoint != null) {
                    var user_json = await GetAPIResult(client,user_endpoint);
                    tblck_rawInfo.Text = user_json;
                    var user = JsonNode.Parse(user_json);
                    if (user!= null) {
                        tblckSuccess.Text = $@"Hello , {user["fullname"]}.{Environment.NewLine}Your account is -{user["account_state"]}";
                    }
                }
                return true;
            } catch (Exception ex) {
                throw;
            }
        }

        private async Task<string> GetAPIResult(IClient client,string endpoint) {
            var result = await client
                     .WithEndPoint(endpoint)
                     .InheritAuthentication()
                     .GetAsync();
            var str_result = await result.AsStringResponseAsync();
            return str_result.Content;
        }

        private async void Button_Click(object sender, RoutedEventArgs e) {
            try {
                btnLogin.IsEnabled = false;
                string msg = string.Empty;
                APIService.port = string.IsNullOrWhiteSpace(tbxPort.Text) ? "9780" : tbxPort.Text;
                tblock_msg.Text = "Initiating listener";
                Task.Run(() => APIService.InitiateSelfHostNetCore()); //host runs and blocks the thread , so don't await this call

                int attempts = 0;
                await LogInCall();
            } catch (Exception ex) {
                throw ex;
            } finally {
                btnLogin.IsEnabled = true;
            }
        }

        private async Task<bool> LogInCall() {
            try {
                var client = ClientStore.GetClient("betaclient");
                var _res3 = await client.SendAsync(Haley.Enums.Method.POST);
                var _res2 = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/"));

                client.SetAuthenticator(new OAuth1Provider(GetNewToken())); //Set for all 
                //client.SetAuthenticator(new Haley.Utils.TokenAuthenticator().SetToken(_header,"OAuth")); //Set for all 
                var _res4 = await client
                            .WithEndPoint($@"oauth/request_token")
                            .InheritAuthentication() //Inherit from parent
                            .SetAuthParam(new OAuth1TokenParam() 
                                    { RequestType = OAuthRequestType.RequestToken,
                                     CallBackURL = new Uri($@"http://localhost:{APIService.port}/api/callback/authorised")})
                            .SendAsync(Haley.Enums.Method.POST);


                var result = await _res4.AsStringResponseAsync();
                if (!string.IsNullOrWhiteSpace(result?.Content)) {
                    var _dic = NetUtils.OAuth.ParseQueryParameters(result.Content, null);
                    var oauth_token = _dic[RestConstants.OAuth.Token];
                    var temp_oauth_token_secret = _dic[RestConstants.OAuth.TokenSecret];
                    _temptokens.TryAdd(oauth_token, (temp_oauth_token_secret,string.Empty));
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

        private void UserInfo(object sender, RoutedEventArgs e) {
            GetUserInfo();
        }
    }
}
