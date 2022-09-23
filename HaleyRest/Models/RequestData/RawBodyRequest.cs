using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Abstractions;
using Haley.Enums;

namespace Haley.Models
{
    public class RawBodyRequest : RequestObject, ISerializeRequest, IRequestBody {
        public bool IsSerialized { get; private set; } //Should be set only once to avoid re-serializing again.
        public void SetSerialized() {
            if (!IsSerialized) IsSerialized = true;
        }
        public BodyContentType BodyType { get; set; }
        public StringContentFormat StringBodyFormat { get; set; }
        public string FileName { get; set; }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// </param>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>
        /// <param name="type"></param>
        /// <param name="body_type"></param>
        public RawBodyRequest(object value, bool is_serialized = false,BodyContentType body_type = BodyContentType.StringContent):base(value)
        {
            BodyType = body_type;
            IsSerialized = is_serialized;
            StringBodyFormat = StringContentFormat.Json;
        }
    }
}
