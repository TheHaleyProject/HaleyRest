using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.Utils;
using Haley.Rest;
using System.Net.Http.Headers;


Console.WriteLine("Hello, World!");
var res =CallMethod().Result;


async Task<bool> CallMethod() {
    try {
        //var client = new RestClient("https://daep.withbc.com/oauth/request_token");
        //client.Timeout = -1;
        //var request = new RestRequest(Method.POST);
        //request.AddHeader("Authorization", "OAuth oauth_consumer_key=\"4579bfc5-0671-4087-bed3-00a41b5cff8c\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"1663964089\",oauth_nonce=\"tVRCGahGmhd\",oauth_version=\"1.0\",oauth_signature=\"AbGl%2FvK6sOSkeytemZ3TMNZWh9o%3D\"");
        //request.AddHeader("Cookie", "awesome_cookie=1663964089.39");
        //IRestResponse response = client.Execute(request);
        //Console.WriteLine(response.Content);

        var _httpclient = new HttpClient();
        _httpclient.BaseAddress = new Uri($@"https://daep.withbc.com/oauth/request_token");
        using (var requestMessage =
            new HttpRequestMessage()) {
            //requestMessage.Headers.Authorization =
            //    new AuthenticationHeaderValue("OAuth", "oauth_consumer_key=\"4579bfc5-0671-4087-bed3-00a41b5cff8c\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"1663966051\",oauth_nonce=\"9evXnN9sTUE\",oauth_version=\"1.0\",oauth_signature=\"08oTJpE%2BOX9Igf%2F4X0SuykppiCk%3D\"");
            _httpclient.Timeout = TimeSpan.FromSeconds(1600);
            requestMessage.Headers.Add("Authorization", "OAuth oauth_consumer_key=\"4579bfc5-0671-4087-bed3-00a41b5cff8c\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"1663969153\",oauth_nonce=\"KXX90Sy5Dpm\",oauth_version=\"1.0\",oauth_signature=\"Y64dV6y0lw2xN7CWPFifYHqnxX8=\"");
            requestMessage.Content = new StringContent(string.Empty);
            //requestMessage.Headers.Add("Cookie", "awesome_cookie=1663967942.64");
            //requestMessage.Headers.Add("User-Agent", "EdgeLocal");
            //requestMessage.Headers.Add("Host", $@"daep.withbc.com");
            //requestMessage.Headers.Add("Accept", "application/json");
            //requestMessage.Headers.Add("Accept-Encoding", "gzip,deflat,br");
            ////requestMessage.Headers.Add("Content-Length", "0");
            //requestMessage.Headers.Add("Accept", "*/*");
            //requestMessage.Headers.Add("Cache-Control", "no-cache");
            //requestMessage.Headers.Add("Connection", "keep-alive");
            requestMessage.Method = HttpMethod.Post;
            var result = await _httpclient.SendAsync(requestMessage);
        }

        //var client = new MicroClient("https://daep.withbc.com");
        //client.AddRequestAuthentication("oauth_consumer_key=\"4579bfc5-0671-4087-bed3-00a41b5cff8c\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"1663965208\",oauth_nonce=\"sxxdWnS82RP\",oauth_version=\"1.0\",oauth_signature=\"zEJeuzHEeeLp9HK7pZ%2FERcWItvI%3D\"", "OAuth");
        //client.AddRequestHeaders("Cookie", "awesome_cookie=1663964753.45");
        //client.AddRequestHeaders("Accept", "*/*");
        //client.AddRequestHeaders("Connection", "keep-alive");
        //var _res = await client.PostObjectAsync("oauth/request_token", null);
        //Console.WriteLine(_res.OriginalResponse.ToString());
        return true;
    }
    catch (Exception ex) {
        Console.WriteLine(ex.ToString());
        return false;
    }
    
}
