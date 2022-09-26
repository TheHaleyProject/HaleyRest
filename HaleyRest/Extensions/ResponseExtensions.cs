using Haley.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Haley.Abstractions;
using System.Threading.Tasks;

namespace Haley.Utils {
    public static class ResponseExtensions
    {
        public static async Task<StringResponse> AsStringResponse(this IResponse response) {
            if (response is StringResponse) return response as StringResponse;
            var result = new StringResponse(null).UpdateResponse(response) as StringResponse;
            return await result.FetchContent() as StringResponse;
        }

        public static async Task<StreamResponse> AsStreamReponse(this IResponse response) {
            if (response is StreamResponse) return response as StreamResponse;
            var result = new StreamResponse(null).UpdateResponse(response) as StreamResponse;
            return await result.FetchContent() as StreamResponse;
        }

        public static async Task<ByteArrayResponse> AsByteArrayResponse(this IResponse response) {
            if (response is ByteArrayResponse) return response as ByteArrayResponse;
            var result = new ByteArrayResponse(null).UpdateResponse(response) as ByteArrayResponse;
            return await result.FetchContent() as ByteArrayResponse;
        }
    }
}
