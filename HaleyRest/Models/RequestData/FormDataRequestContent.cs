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

        public void Add(List<IQueryRequestContent> queryParamList) {
            base.UpdateValue(GetDic(queryParamList));
        }

        static Dictionary<string, IRawBodyRequestContent> GetDic(List<IQueryRequestContent> queryParamList) {
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

        public new Dictionary<string, IRawBodyRequestContent> Value => base.Value as Dictionary<string, IRawBodyRequestContent>;

        public FormDataRequestContent(List<IQueryRequestContent> qpmList) : this(GetDic(qpmList)) { }


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
