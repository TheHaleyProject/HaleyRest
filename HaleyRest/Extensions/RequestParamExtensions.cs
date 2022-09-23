using Haley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Haley.Utils {
    public static class RequestParamExtensions {
        public static  IEnumerable<RequestParam> ToRequestParams(this Dictionary<string,string> parameters, bool encode_values) {
            List<RequestParam> result = new List<RequestParam>();
            if (parameters == null) return result;
            foreach (var kvp in parameters) {
                result.Add(kvp.ToRequestParam(encode_values));
            }
            return result;
        }

        public static RequestParam ToRequestParam(this KeyValuePair<string,string> kvp, bool encode_values) {
            if (string.IsNullOrWhiteSpace(kvp.Key)) return null;
            var data = kvp.Value;
            if (encode_values && !string.IsNullOrWhiteSpace(data)) data = Uri.EscapeDataString(data);
            return new RequestParam(kvp.Key, data);
        }

        public static IEnumerable<RequestParam> RemoveNulls(this IEnumerable<RequestParam> parameters) {
            List<RequestParam> result = new List<RequestParam>();
            if (parameters == null) return result;
            result = parameters.Where(p => p != null)?.ToList();
            return result;
        }
    }
}
