using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;


namespace Haley.Models
{
    public abstract class ResponseBase : IResponse
    {
        public HttpResponseMessage OriginalResponse { get; private set; }
        public HttpContent OriginalContent => OriginalResponse == null ? null : OriginalResponse.Content;

        public bool IsSuccessStatusCode => OriginalResponse == null ? false : OriginalResponse.IsSuccessStatusCode;
        public IRequest Request {get; internal set;}
        public virtual void UpdateResponse(IResponse source_response)
        {
            if (source_response == null) return;
            this.OriginalResponse = source_response.OriginalResponse;
        }
        protected ResponseBase(HttpResponseMessage response) { OriginalResponse = response; }
    }
}
