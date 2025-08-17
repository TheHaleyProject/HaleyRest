using Haley.Abstractions;
using System.Collections;
using System.Collections.Generic;
using Haley.Utils;
using System.Security.Cryptography;
using System.Net.Http;

namespace Haley.Models {
    public class FormDataRequestContent : HttpRequestContent, IFormDataRequestContent {
        //public IEnumerable<RequestParam> Parameters { get; set; }
        //public void SetAllEncoded() {
        //  if(Parameters != null) {
        //        Parameters.ToList()?.ForEach(p => p.SetAsURLDecoded());
        //    }
        //}

        public void Add(List<IQueryRequestContent> queryParamList) {
            base.UpdateValue(GetDic(queryParamList));
        }

        public void Add(Dictionary<string,object> dictionary) {
            base.UpdateValue(GetDic(dictionary));
        }

        public static Dictionary<string, IRawBodyRequestContent> GetDic(Dictionary<string,object> source) {
            var result = new Dictionary<string, IRawBodyRequestContent>();

            foreach (var item in source) {
                if (item.Value is IEnumerable<object> list && !(item.Value is string)) {
                    foreach (var val in list) {
                        string key = item.Key;
                        if (!result.ContainsKey(key)) {
                            result.Add(key, new RawBodyRequestContent(new List<object>() { val}, false));
                        } else {
                            result[key].Append(val); 
                        }
                    }
                } else {
                    if (!result.ContainsKey(item.Key)) {
                        result.Add(item.Key, new RawBodyRequestContent(item.Value, true));
                    } else {
                        result[item.Key] = new RawBodyRequestContent(item.Value, true);
                    }
                }
            }
            return result;
        }

        public static Dictionary<string, IRawBodyRequestContent> GetDic(List<IQueryRequestContent> queryParamList) {
            var result = new Dictionary<string, IRawBodyRequestContent>();
            foreach (var item in queryParamList) {
                if (!result.ContainsKey(item.Key)) {
                    result.Add(item.Key, new RawBodyRequestContent(item.Value,true));
                } else {
                    result[item.Key] = new RawBodyRequestContent(item.Value,true);
                }
            }
            return result;
        }

        public static Dictionary<string, IRawBodyRequestContent> GetDic(FormUrlEncodedContent content,string key) {
            var result = new Dictionary<string, IRawBodyRequestContent>();
            result.Add(key, new RawBodyRequestContent(content, true));
            return result;
        }

        public new Dictionary<string, IRawBodyRequestContent> Value => base.Value as Dictionary<string, IRawBodyRequestContent>;
        public FormDataRequestContent(Dictionary<string, object> source) : this(GetDic(source)) { }
        public FormDataRequestContent(List<IQueryRequestContent> qpmList) : this(GetDic(qpmList)) { }
        public FormDataRequestContent(FormUrlEncodedContent content,string key) : this(GetDic(content,key)) { }
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        public FormDataRequestContent(Dictionary<string, IRawBodyRequestContent> value) : base(value) {
            if (value == null) {
                base.UpdateValue(new Dictionary<string, IRawBodyRequestContent>());
            }
        }
    }
}
