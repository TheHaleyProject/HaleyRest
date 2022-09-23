using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Abstractions;

namespace Haley.Models
{
    public class QueryParam : RequestObject, IRequestQuery
    {
        public string Key { get; set; }
        public new string Value => base.Value as string; //Hiding the base Value.
        public bool ShouldEncode { get; set; }
        public bool IsEncoded { get; private set; }

        public void SetEncoded() {
            if (!IsEncoded) IsEncoded = true; //Set only once. Cannot encode or change it again back to false.
        }

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="key">Key to add.
        /// </param>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>
        public QueryParam(string key, string value,bool should_encode = true):base (value)
        {
            Key = string.IsNullOrWhiteSpace(key)?"id":key;
            ShouldEncode = should_encode; //By default, the query parameters are encoded
            IsEncoded = false;
        }
    }
}
