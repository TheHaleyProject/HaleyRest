﻿using System.Net.Http;

namespace Haley.Abstractions {
    public interface IResponse {
        string Message { get; }
        bool IsContentEncoded { get; }
        HttpResponseMessage OriginalResponse { get; }
        bool IsSuccessStatusCode { get; }
        HttpContent OriginalContent { get; }
        IResponse UpdateResponse(IResponse source_response);
    }
}
