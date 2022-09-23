﻿using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class OAuthRequestInfo
    {
        private Method _method = Method.GET;
        public Method Method {
            get { return _method; }
            set { _method = value; }
        }
        public string RequestURL { get; set; }
        public string CallBackURL { get; set; }
        public string SessionHandle { get; set; }
        public SortedDictionary<string,string> Parameters { get; set; }
        public OAuthRequestInfo SetRequestURL(string request_url) {
            RequestURL = request_url;
            return this;
        }

        public OAuthRequestInfo() { Parameters = new SortedDictionary<string, string>(); }
    }
}
