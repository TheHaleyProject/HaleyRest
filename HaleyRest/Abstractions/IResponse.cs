using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace Haley.Abstractions
{
    public interface IResponse
    {
        IRequest Request { get; }
        HttpResponseMessage OriginalResponse { get; }
        bool IsSuccessStatusCode { get; }
        HttpContent OriginalContent { get; }
        void UpdateResponse(IResponse source_response);
    }
}
