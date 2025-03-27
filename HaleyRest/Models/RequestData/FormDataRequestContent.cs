using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models {
    public class FormDataRequestContent : HttpRequestContent, IFormDataRequestContent {
        //public IEnumerable<RequestParam> Parameters { get; set; }
        //public void SetAllEncoded() {
        //  if(Parameters != null) {
        //        Parameters.ToList()?.ForEach(p => p.SetAsURLDecoded());
        //    }
        //}

        public void Add(List<QueryParam> queryParamList) {
            base.UpdateValue(GetDic(queryParamList));
        }

        static Dictionary<string, RawBodyRequestContent> GetDic(List<QueryParam> queryParamList) {
            var result = new Dictionary<string, RawBodyRequestContent>();
            foreach (var item in queryParamList) {
                if (!result.ContainsKey(item.Key)) {
                    result.Add(item.Key, new RawBodyRequestContent(item.Value,true));
                } else {
                    result[item.Key] = new RawBodyRequestContent(item.Value,true);
                }
            }
            return result;
        }

        public new Dictionary<string, RawBodyRequestContent> Value => base.Value as Dictionary<string, RawBodyRequestContent>;

        public FormDataRequestContent(List<QueryParam> queryParamList) : this(GetDic(queryParamList)) { }

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        public FormDataRequestContent(Dictionary<string, RawBodyRequestContent> value) : base(value) {
            if (value == null) {
                base.UpdateValue(new Dictionary<string, RawBodyRequestContent>());
            }
        }
    }
}
