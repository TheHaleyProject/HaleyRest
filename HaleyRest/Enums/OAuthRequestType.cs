﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Enums {
    public enum OAuthRequestType {
        AccessToken,
        RequestToken, //This will be the first request
        ForProtectedResource,
    }
}
