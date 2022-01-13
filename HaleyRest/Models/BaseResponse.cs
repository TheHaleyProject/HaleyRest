using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;


namespace Haley.Models
{
    public class BaseResponse : IResponse
    {
        public HttpResponseMessage OriginalResponse { get; set; }
        public bool IsSuccess => OriginalResponse == null ? false : OriginalResponse.IsSuccessStatusCode;
        public HttpContent Content => OriginalResponse == null ? null : OriginalResponse.Content;
        public BaseResponse() { }
    }
}
