using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Abstractions;

namespace Haley.Models
{
    public class MultiPartFormRequest : RequestObject,IRequestBody {
        //public IEnumerable<RequestParam> Parameters { get; set; }
        //public void SetAllEncoded() {
        //  if(Parameters != null) {
        //        Parameters.ToList()?.ForEach(p => p.SetEncoded());
        //    }
        //}

        public new Dictionary<string, RawBodyRequest> Value => base.Value as Dictionary<string, RawBodyRequest>;

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        public MultiPartFormRequest(Dictionary<string, RawBodyRequest> value):base(value)
        {
            if(value == null) {
                base.UpdateValue(new Dictionary<string, RawBodyRequest>());
            }
        }
    }
}
