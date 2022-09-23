using System;
using System.Net;
using System.Net.Http;

namespace Haley.Models
{
    /// <summary>
    /// Content response with generic variable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SerializedResponse<T> : StringResponse where T: class
    {
        public T SerializedContent { get; set; }
        public SerializedResponse() { }
    }
}
