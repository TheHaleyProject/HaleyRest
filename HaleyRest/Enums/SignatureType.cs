using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Haley.Enums {
    public enum SignatureType {
        [Description("HMAC-SHA1")]
        HMACSHA1,
        [Description("HMAC-SHA256")]
        HMACSHA256,
        [Description("HMAC-SHA512")]
        HMACSHA512,
        [Description("RSA-SHA1")]
        RSASHA1,
        [Description("RSA-SHA256")]
        RSASHA256,
        [Description("RSA-SHA512")]
        RSASHA512,
        [Description("PLAINTEXT")]
        PLAINTEXT
    }
}
