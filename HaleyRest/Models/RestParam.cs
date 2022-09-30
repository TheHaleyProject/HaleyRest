using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Haley.Utils;

namespace Haley.Models {
    public class RestParam {
        public string Key { get; private set; }
        public string Value { get; set; }
        public bool CanEncodeValue { get; set; }
        public RestParam(string key, string value) {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            CanEncodeValue = true;
        }

        public RestParam():this(string.Empty,string.Empty) { }

        public override string ToString() {
            return $@"{Key}:{Value}";
        }
    }
}
