using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Haley.Models {
    public static class ForwardContextExtensions {
        private const string Key = "Haley.ForwardContext";

        public static void SetForwardContext(this HttpRequestMessage req, ForwardContext ctx)
            => req.Properties[Key] = ctx;

        public static bool TryGetForwardContext(this HttpRequestMessage req, out ForwardContext ctx) {
            ctx = null;
            if (req.Properties.TryGetValue(Key, out var obj) && obj is ForwardContext fc) { ctx = fc; return true; }
            return false;
        }
    }
}
