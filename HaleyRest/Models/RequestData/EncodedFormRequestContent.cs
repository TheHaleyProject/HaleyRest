using Haley.Abstractions;
using Haley.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Haley.Models {
    public class EncodedFormRequestContent : HttpRequestContent, IEncodedFormRequestContent {

        public new IList<IQueryRequestContent> Value => base.Value as IList<IQueryRequestContent>;

        public string GetEncodedBodyContent() {

            var encoded = Value?.Select(p => $"{NetUtils.URLSingleEncode(p.Key, p.IsURLDecoded ? p.Key : null)}={NetUtils.URLSingleEncode(p.Value, p.IsURLDecoded ? p.Value : null)}");
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
