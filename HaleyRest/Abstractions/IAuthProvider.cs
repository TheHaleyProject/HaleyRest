using Haley.Utils;
using System;
using System.Net.Http;

namespace Haley.Abstractions {
    /// <summary>
    /// To prepare authentication token and attach as a header for Rest Request
    /// <list type="table">
    /// <item>
    /// <description><see cref="OAuth1Provider"/> - For performing OAuth1.0 kind of authentication. Need consumer_key, consumer_secret</description>
    /// </item>
    /// <item>
    /// <description><see cref="TokenAuthProvider"/> - For storing and sending Bearer Consumer authentications. Can change the prefix as required.</description>
    /// </item>
    /// </list>
    /// </summary>
    public interface IAuthProvider {
        string GenerateToken(Uri baseuri, HttpRequestMessage request, IAuthParam auth_param);
        void ClearToken();
    }
}
