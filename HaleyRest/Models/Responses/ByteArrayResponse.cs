using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;

namespace Haley.Models
{
    public class ByteArrayResponse : RestResponse<byte[]>
    { 
        public ByteArrayResponse(HttpResponseMessage response) : base(response) { var arg = FetchContent().Result; }
        public ByteArrayResponse() : this(null) { }
    }
}
