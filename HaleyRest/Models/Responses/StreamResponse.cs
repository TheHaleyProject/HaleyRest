using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;
using System.IO;

namespace Haley.Models
{
    public class StreamResponse : RestResponse<Stream> 
    {
        public StreamResponse(HttpResponseMessage response) : base(response) { var arg = FetchContent().Result; }
        public StreamResponse():this(null) { }
    }
}
