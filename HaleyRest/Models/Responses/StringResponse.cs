using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;

namespace Haley.Models
{
    public class StringResponse : RestResponse<string> 
    {
        public StringResponse(HttpResponseMessage response) : base(response) {var arg = FetchContent().Result; }
        public override string ToString() {
            return base.Content;
        }
        public StringResponse() : this(null) { }
    }
}
