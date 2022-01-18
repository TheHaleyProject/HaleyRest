using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace Haley.Abstractions
{
    public interface IResponse
    {
        HttpResponseMessage OriginalResponse { get; set; }
        bool IsSuccessStatusCode { get; }
        HttpContent Content { get; }
        void CopyTo(IResponse input);
    }
}
