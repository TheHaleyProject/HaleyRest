using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;

namespace Haley.Models
{
    public class RequestBodyRaw : RequestArgsBase
    {
        public BodyContentType BodyType { get; set; }
        public StringContentFormat StringBodyFormat { get; set; }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// </param>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>
        /// <param name="type"></param>
        /// <param name="body_type"></param>
        public RequestBodyRaw(object value, bool is_serialized = false,BodyContentType body_type = BodyContentType.StringContent):base(value,is_serialized)
        {
            IsSerialized = is_serialized;
            BodyType = body_type;
            StringBodyFormat = StringContentFormat.Json;
        }
    }
}
