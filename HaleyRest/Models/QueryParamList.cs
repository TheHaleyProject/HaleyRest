using Haley.Abstractions;
using Haley.Utils;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Haley.Models {
    public class QueryParamList : List<IQueryRequestContent> {

        #region Getters
        public IQueryRequestContent this[string key] {
            get {
                var allparams = this.Where(p => p.Key.Equals(key));
                if (allparams.Count() == 0) {
                    return null;
                }

                if (allparams.Count() == 1) {
                    return allparams.Single();
                }

                // We have more than one value for same key, we need to concatenate and provide the values over.
                var result = string.Join(",", allparams.Select(p => p.Value).ToArray());
                return new QueryParam(key, result); //Instead of one value, we give all values merged together.
            }
        }

        public IEnumerable<string> Keys {
            get {
                return this.Select(p => p.Key)?.Distinct();
            }
        }

        public IEnumerable<string> Values {
            get {
                return this.Select(p => p.Value); //Not distinct.
            }
        }

        #endregion

        #region Constructors
        public QueryParamList() { }

        public QueryParamList(IEnumerable<IQueryRequestContent> @params) {
            AddRange(@params);
        }
        public QueryParamList(NameValueCollection @params) {
            AddCollection(@params);
        }

        public QueryParamList(Dictionary<string, string> @params) {
            this.AddDictionary(@params);
        }
        #endregion

        #region Methods

        public void Add(string key, string value) => Add(new QueryParam(key, value));

        public void AddRange(IEnumerable<IQueryRequestContent> @params) {
            if (@params == null) return;
            base.AddRange(@params);
        }
        public void AddCollection(NameValueCollection @params) {
            //When you add a namevaluecollection, all values will be considered as URLDecoded = false.
            if (@params == null) return;
            foreach (var key in @params.AllKeys) {
                var _key = key;
                var _value = @params[key];
                var param = new QueryParam(_key, _value) { };
                base.Add(param);
            }
        }

        public void AddDictionary(IDictionary<string, string> @params) {
            if (@params == null) return;
            foreach (var item in @params) {
                var _key = item.Key;
                var _value = item.Value;
                var param = new QueryParam(_key, _value) { };
                base.Add(param);
            }
        }

        public void SortByKey() {
            this.Sort((item1, item2) => item1.Key.CompareTo(item2.Key));
        }

        public string GetConcatenatedString(string splitter = "&", string kvp_merger = "=", bool should_sort = true, bool urlEncode = true) {
            var baseArr = new QueryParam[this.Count()];
            this.CopyTo(baseArr); //So we don't mess with the existing values. Copy to a new array.
            var copiedList = baseArr.ToList();

            if (should_sort) {
                copiedList.Sort((item1, item2) => item1.Key.CompareTo(item2.Key)); //Compare the keys to sort. //Still this keep hello100 as small than hello2. Need to optimize later to use better way to compre alphanumeric values (check haley utils alphanumeric comparer).
                //todo: If we have two same keys (some OAuth allows this), we need to again sort by values.
            }

            StringBuilder sb = new StringBuilder();
            var total = copiedList.Count();
            int i = 0;
            foreach (var item in copiedList) {
                var key = item.Key;
                var value = item.Value;

                if (urlEncode) {
                    key = NetUtils.URLSingleEncode(key, item.IsURLDecoded ? item.Key : null);
                    value = NetUtils.URLSingleEncode(value, item.IsURLDecoded ? item.Value : null);
                }

                //key
                sb.Append(key);
                //= sign
                sb.Append(kvp_merger);
                //value
                sb.Append(value);
                // & splitter if needed
                i++;
                if (i < total) {
                    sb.Append(splitter);
                }
            }
            return sb.ToString();
        }
        #endregion

    }
}
