using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace Haley.Abstractions
{
    public interface IResponse
    {
        HttpResponseMessage OriginalResponse { get; set; }
        bool IsSuccess { get; }
        HttpContent Content { get; }
    }
}
