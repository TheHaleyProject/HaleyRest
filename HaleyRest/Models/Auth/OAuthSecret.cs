using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Haley.Models
{
    //FOLLOWS : https://www.rfc-editor.org/rfc/rfc5849
    public class OAuthToken {
        public string Key { get; private set; }
        public string Secret { get; private set; }
        public OAuthToken Update(string key, string secret) {
            Key = key ?? string.Empty;
            Secret = secret ?? string.Empty;
            return this;
        }
        public OAuthToken(string key, string secret) {
            Update(key, secret);
        }
        public OAuthToken() : this(string.Empty, string.Empty) { }
    }
}
