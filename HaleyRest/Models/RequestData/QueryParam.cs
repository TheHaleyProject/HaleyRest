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
        public bool IsURLDecoded { get; private set; }

        public IRequestQuery SetAsURLDecoded() {
            IsURLDecoded = true; //Set only once. Cannot encode or change it again back to true.
            return this;
        }

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="key">Key to add.
        /// </param>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>
        public QueryParam(string key, string value):base (value)
        {
            Key = string.IsNullOrWhiteSpace(key)?"id":key;
            //By default, the query parameters are not locked. Which means, it will be marked for single encode (decode to the final value and then encode once).
        }

        public override string ToString() {
            return $@"{Key}={Value}";
        }
    }
}
