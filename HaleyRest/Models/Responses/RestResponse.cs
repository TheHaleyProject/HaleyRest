using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;

namespace Haley.Models
{
    public class RestResponse : RestResponse<string>
    {
        public RestResponse(HttpResponseMessage response):base(response) { }
    }

    public class RestResponse<T> : ResponseBase where T : class
    {
        public T Content { get; private set; }

        public void SetContent(T content) {
            Content = content;
        }

        public override void UpdateResponse(IResponse source) {
            base.UpdateResponse(source);
            if (source is RestResponse<T> source_generic_res) {
                this.Content = source_generic_res.Content;
            }
        }
        public RestResponse(HttpResponseMessage response):base(response) { }
    }
}
