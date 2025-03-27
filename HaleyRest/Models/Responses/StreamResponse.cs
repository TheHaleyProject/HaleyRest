using System.IO;
using System.Net.Http;

namespace Haley.Models {
    public class StreamResponse : RestResponse<Stream> {
        public StreamResponse(HttpResponseMessage response) : base(response) { var arg = FetchContent().Result; }
        public override string ToString() {
            return base.OriginalContent?.ToString();
        }
        public StreamResponse() : this(null) { }
    }
}
