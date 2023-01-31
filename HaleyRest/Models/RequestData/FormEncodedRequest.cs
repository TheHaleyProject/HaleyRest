using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Abstractions;
using System.Net;

namespace Haley.Models
{
    public class FormEncodedRequest : RequestObject,IRequestBody {

        public new IList<QueryParam> Value => base.Value as IList<QueryParam>;

        public string GetEncodedBodyContent() {
            var encoded = Value?.Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}");
            //*.Replace("%20", "+") */
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
