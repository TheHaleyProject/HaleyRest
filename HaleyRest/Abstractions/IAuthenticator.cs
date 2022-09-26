using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using Haley.Models;
using Haley.Utils;

namespace Haley.Abstractions
{
    /// <summary>
    /// To prepare authentication token and attach as a header for Rest Request
    /// <list type="table">
    /// <item>
    /// <description><see cref="OAuth1Authenticator"/> - For performing OAuth1.0 kind of authentication. Need consumer_key, consumer_secret</description>
    /// </item>
    /// <item>
    /// <description><see cref="TokenAuthenticator"/> - For storing and sending Bearer Token authentications. Can change the prefix as required.</description>
    /// </item>
    /// </list>
    /// </summary>
    public interface IAuthenticator{
        
    }
}
