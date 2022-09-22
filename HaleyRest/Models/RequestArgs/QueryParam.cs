using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;

namespace Haley.Models
{
    public class QueryParam : RequestArgsBase
    {
        public string Key { get; set; }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="key">Key to add.
        /// </param>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>
        public QueryParam(string key, object value, bool is_serialized = false):base (value, is_serialized)
        {
            Key = string.IsNullOrWhiteSpace(key)?"id":key;
            Value = value;
            IsSerialized = is_serialized;
        }
    }
}
