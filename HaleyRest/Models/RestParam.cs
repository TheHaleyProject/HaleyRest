using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Haley.Utils;
using Haley.Abstractions;

namespace Haley.Models {
    public class RestParam : IEncode<RestParam> {
        public string Key { get; private set; }
        public string Value { get; set; }
        public bool CanEncode { get; set; }

        public RestParam(string key, string value) {
            Key = key ?? string.Empty;
            Value = value ?? string.Empty;
            CanEncode = true;
        }

        public RestParam():this(string.Empty,string.Empty) { }

        public override string ToString() {
            return $@"{Key}={Value}";
        }

        public RestParam SetEncoded() {
            CanEncode = false;
            return this;
        }
    }
}
