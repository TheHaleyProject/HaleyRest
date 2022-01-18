using Haley.Abstractions;
using System;
using System.Net;
using System.Net.Http;

namespace Haley.Models
{
    public class StringResponse : BaseResponse
    {
        public string StringContent { get; set; }
        public override void CopyTo(IResponse input)
        {
            base.CopyTo(input);
            if (input is StringResponse strresp)
            {
                strresp.StringContent = this.StringContent;
            }
        }
        public StringResponse() { }
    }
}
