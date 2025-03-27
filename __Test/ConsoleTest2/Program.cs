using Haley.Rest;
using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;

// See https://aka.ms/new-console-template for more information
// https://core360.daep.ae/pinnacle_asset_info/add_asset_info/
Console.WriteLine("Hello, World!");
var epoint = @"pinnacle_asset_info/add_asset_info/";
var _client = new FluentClient(@"https://core360.daep.ae/");

var qpl = new QueryParamList();
qpl.Add("cpu_id", "dadaefas55d");
qpl.Add("user_id", "ttt4556df");
qpl.Add("user_name", "Lingam Test 65rf");
qpl.Add("asset_info", @"{""hello"":""test""}");
qpl.Add("user_name", "ertrert");
qpl.Add("asset_hash", "23eaw234qwrfa");
var res = await _client.WithEndPoint(epoint)
    .WithForm(new EncodedFormRequestContent(qpl))
    .PostAsync();
var resS = await res.AsStringResponseAsync();
Console.WriteLine("Processed");
