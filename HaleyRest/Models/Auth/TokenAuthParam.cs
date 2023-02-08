using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Haley.Utils;
using Haley.Abstractions;

namespace Haley.Models {
    public class TokenAuthParam : IAuthParam {
        public object[] Arguments { get; set; }
        public string Prefix { get; set; }
        public TokenAuthParam(string prefix = "Bearer") { Prefix = prefix; }
    }
}
