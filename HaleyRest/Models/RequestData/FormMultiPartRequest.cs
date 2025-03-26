using Haley.Abstractions;
using System.Collections.Generic;

namespace Haley.Models
{
    public class FormMultiPartRequest : RequestObject,IRequestBody {
        //public IEnumerable<RequestParam> Parameters { get; set; }
        //public void SetAllEncoded() {
        //  if(Parameters != null) {
        //        Parameters.ToList()?.ForEach(p => p.SetAsURLDecoded());
        //    }
        //}

        public void Add(List<QueryParam> queryParamList) {
            base.UpdateValue(GetDic(queryParamList));
        }

        static Dictionary<string,RawBodyRequest> GetDic(List<QueryParam> queryParamList) {
            var result = new Dictionary<string, RawBodyRequest>();
            foreach (var item in queryParamList) {
                if (!result.ContainsKey(item.Key)) {
                    result.Add(item.Key, new RawBodyRequest(item.Value));
                } else {
                    result[item.Key] = new RawBodyRequest(item.Value);
                }
            }
            return result;
        }

        public new Dictionary<string, RawBodyRequest> Value => base.Value as Dictionary<string, RawBodyRequest>;

        public FormMultiPartRequest(List<QueryParam> queryParamList): this(GetDic(queryParamList)){}

        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// <param name="value"></param>
        public FormMultiPartRequest(Dictionary<string, RawBodyRequest> value):base(value)
        {
            if(value == null) {
                base.UpdateValue(new Dictionary<string, RawBodyRequest>());
            }
        }
    }
}
