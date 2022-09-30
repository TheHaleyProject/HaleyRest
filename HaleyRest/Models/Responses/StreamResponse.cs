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
        public override string ToString() {
            return base.OriginalContent?.ToString();
        }
        public StreamResponse():this(null) { }
    }
}
