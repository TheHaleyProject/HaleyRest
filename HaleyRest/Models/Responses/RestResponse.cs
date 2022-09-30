using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

namespace Haley.Models
{
    public class RestResponse<T> : BaseResponse  where T : class
    { 
        public T Content { get; private set; }
        string _stringcontent;
        Func<string, T> _converter;

        public RestResponse<T> SetContent(T content) {
            Content = content;
            return this;
        }
        public async Task<RestResponse<T>> FetchContent() {
            if (base.OriginalContent == null) return this;
            if (typeof(T) == typeof(byte[])) {
                Content = await base.OriginalContent?.ReadAsByteArrayAsync() as T;
            }
            else if (typeof(T) == typeof(string)) {
                Content = await base.OriginalContent?.ReadAsStringAsync() as T;
            }
            else if (typeof(T) == typeof(Stream)) {
                Content = await base.OriginalContent?.ReadAsStreamAsync() as T;
            }
            else {
                //Get it as string and then try to convert it to the object using converters of some sort.
                _stringcontent = await base.OriginalContent?.ReadAsStringAsync();
                if (_converter != null) {
                    Content = _converter.Invoke(_stringcontent);
                }
            }
            return this;
        }

        public RestResponse<T> SetConveter(Func<string,T> converter) {
            _converter = converter;
            return this;
        }

        public RestResponse(HttpResponseMessage response) : base(response) { }
    }
}
