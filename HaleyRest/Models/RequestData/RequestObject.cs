using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;

namespace Haley.Models
{
    public abstract class RequestObject
    {
        public string Id { get;}
        public object Value { get; set; }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>

        public RequestObject(object value)
        {
            Id = Guid.NewGuid().ToString();
            Value = value;
        }
    }
}
