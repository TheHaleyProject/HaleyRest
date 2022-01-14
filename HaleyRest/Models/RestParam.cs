﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;

namespace Haley.Models
{
    public class RestParam
    {
        public string Id { get;}
        public string Key { get; set; }
        public object Value { get; set; }
        public bool IsSerialized { get; set; }
        public ParamType ParamType { get; set; }
        public RequestBodyType BodyType { get; set; }
        public string FileName { get; set; }
        //public string HeaderContentType { get; set; }
        //public string HeaderContentDisposition { get; set; }
        public StringContentFormat StringBodyFormat { get; set; }
        public RestParam(string key, object value, bool is_serialized = false, ParamType type = ParamType.QueryString,RequestBodyType body_type = RequestBodyType.StringContent)
        {
            Id = Guid.NewGuid().ToString();
            Key = key;
            Value = value;
            ParamType = type;
            IsSerialized = is_serialized;
            BodyType = body_type;
            FileName = string.Empty;
            StringBodyFormat = StringContentFormat.Json;
        }
    }
}
