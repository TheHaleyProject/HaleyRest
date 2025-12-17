using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Haley.Models {
    public sealed class ForwardContext {
        public string Host { get; }
        public string Proto { get; }
        public string XForwardedFor { get; }

        public ForwardContext(string host, string proto, string xForwardedFor) {
            Host = host ?? string.Empty;
            Proto = proto ?? string.Empty;
            XForwardedFor = xForwardedFor ?? string.Empty;
        }
    }
}
