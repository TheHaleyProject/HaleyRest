using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Abstractions;
using System.Net;
using Haley.Utils;

namespace Haley.Models
{
    public class FormEncodedRequest : RequestObject,IRequestBody {

        public new IList<QueryParam> Value => base.Value as IList<QueryParam>;

        public string GetEncodedBodyContent() {

            var encoded = Value?.Select(p => $"{NetUtils.URLSingleEncode(p.Key, p.IsURLDecoded ? p.Key : null)}={NetUtils.URLSingleEncode(p.Value, p.IsURLDecoded ? p.Value : null)}");
            return string.Join("&", encoded);
        }

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        public FormEncodedRequest(IList<QueryParam> value):base(value)
        {
            if(value == null) {
                base.UpdateValue(new List<QueryParam>());
            }
        }
    }
}
