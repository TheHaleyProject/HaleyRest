using Haley.Models;
using System.Collections.Generic;
using System.Linq;

namespace Haley.Utils {
    public static class RequestParamExtensions {
        public static IEnumerable<QueryParam> ToRequestParams(this Dictionary<string, string> parameters) {
            List<QueryParam> result = new List<QueryParam>();
            if (parameters == null) return result;
            foreach (var kvp in parameters) {
                result.Add(kvp.ToRequestParam());
            }
            return result;
        }

        public static QueryParam ToRequestParam(this KeyValuePair<string, string> kvp) {
            if (string.IsNullOrWhiteSpace(kvp.Key)) return null;
            var data = kvp.Value;
            return new QueryParam(kvp.Key, kvp.Value);
        }

        public static IEnumerable<QueryParam> RemoveNulls(this IEnumerable<QueryParam> parameters) {
            List<QueryParam> result = new List<QueryParam>();
            if (parameters == null) return result;
            result = parameters.Where(p => p != null)?.ToList();
            return result;
        }
    }
}
