using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Abstractions;

namespace Haley.Models
{
    public class FormBodyRequest : RequestObject,IRequestBody {
        public IEnumerable<RequestParam> Parameters { get; set; }
        public void SetAllEncoded() {
          if(Parameters != null) {
                Parameters.ToList()?.ForEach(p => p.SetEncoded());
            }
        }

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        public FormBodyRequest(IEnumerable<RequestParam> parameters = null):base(null)
        {
            Parameters = parameters ?? new List<RequestParam>();
        }
    }
}
