using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;

namespace Haley.Models
{
    public class RequestBodyForm : RequestArgsBase
    {
        public Dictionary<string,string> Data { get; set; }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>
        public RequestBodyForm(object value, bool is_serialized = false):base(value,is_serialized)
        {
            IsSerialized = is_serialized;
        }
    }
}
