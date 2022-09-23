using Haley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Haley.Utils {
    public static class RequestParamExtensions {
        public static  IEnumerable<QueryParam> ToRequestParams(this Dictionary<string,string> parameters, bool encodeKVP) {
            List<QueryParam> result = new List<QueryParam>();
            if (parameters == null) return result;
            foreach (var kvp in parameters) {
                result.Add(kvp.ToRequestParam(encodeKVP));
            }
            return result;
        }

        public static QueryParam ToRequestParam(this KeyValuePair<string,string> kvp, bool encodeKVP) {
            if (string.IsNullOrWhiteSpace(kvp.Key)) return null;
            var data = kvp.Value;
            var key = kvp.Key;
            if (encodeKVP && !string.IsNullOrWhiteSpace(data)) {
                data = Uri.EscapeDataString(data);
                key = Uri.EscapeDataString(key);
            }
            return new QueryParam(key, data);
        }

        public static IEnumerable<QueryParam> RemoveNulls(this IEnumerable<QueryParam> parameters) {
            List<QueryParam> result = new List<QueryParam>();
            if (parameters == null) return result;
            result = parameters.Where(p => p != null)?.ToList();
            return result;
        }
    }
}
