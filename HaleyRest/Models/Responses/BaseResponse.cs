using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;


namespace Haley.Models
{
    public class BaseResponse : IResponse
    {
        public HttpResponseMessage OriginalResponse { get; set; }
        public bool IsSuccessStatusCode => OriginalResponse == null ? false : OriginalResponse.IsSuccessStatusCode;
        public HttpContent Content => OriginalResponse == null ? null : OriginalResponse.Content;
        public virtual void CopyTo(IResponse input)
        {
            if (input == null) return;
            input.OriginalResponse = this.OriginalResponse;
        }

        public BaseResponse() { }
    }
}
