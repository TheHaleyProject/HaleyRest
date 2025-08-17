using Haley.Abstractions;
using Haley.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Haley.Models {
    public class EncodedFormRequestContent : HttpRequestContent, IEncodedFormRequestContent {

        public new IList<IQueryRequestContent> Value => base.Value as IList<IQueryRequestContent>;

        public string GetEncodedBodyContent() {

            var encoded = Value?.Select(p => {
                var value = p.Value is string str ? NetUtils.URLSingleEncode(str, p.IsURLDecoded ? str : null) : p.Value;
                return $"{NetUtils.URLSingleEncode(p.Key, p.IsURLDecoded ? p.Key : null)}={value}";
            });
            return string.Join("&", encoded);
        }

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        public EncodedFormRequestContent(IList<IQueryRequestContent> value) : base(value) {
            if (value == null) {
                base.UpdateValue(new List<IQueryRequestContent>());
            }
        }
    }
}
