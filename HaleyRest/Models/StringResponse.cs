using System;
using System.Net;
using System.Net.Http;

namespace Haley.Models
{
    public class StringResponse : BaseResponse
    {
        public string StringContent { get; set; }
        public StringResponse() { }
    }
}
