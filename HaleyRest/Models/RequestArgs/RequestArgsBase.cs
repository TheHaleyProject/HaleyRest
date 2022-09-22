using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;

namespace Haley.Models
{
    public abstract class RequestArgsBase
    {
        public string Id { get;}
        public object Value { get; set; }
        public bool IsSerialized { get; set; }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>

        public RequestArgsBase(object value, bool is_serialized = false)
        {
            Id = Guid.NewGuid().ToString();
            Value = value;
            IsSerialized = is_serialized;
        }
    }
}
