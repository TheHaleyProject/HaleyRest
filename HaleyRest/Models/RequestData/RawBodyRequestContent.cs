using Haley.Abstractions;
using Haley.Enums;

namespace Haley.Models {
    public class RawBodyRequestContent : HttpRequestContent, ISerializeRequest, IRawBodyRequestContent {
        public bool IsSerialized { get; private set; } //Should be set only once to avoid re-serializing again.
        public void SetSerialized() {
            if (!IsSerialized) IsSerialized = true;
        }
        public BodyContentType BodyType { get; set; }
        /// <summary>
        /// This will be shared with the HTTP Request. Can be used for sending the request with file name.
        /// </summary>
        public string Title { get; set; }

        public string MIMEType { get; set; }
        public bool OverrideMIMETypeAutomatically { get; set; } = true;
        /// <summary>
        /// Rest Param Object
        /// </summary>
        /// </param>
        /// <param name="value"></param>
        /// <param name="is_serialized"></param>
        /// <param name="type"></param>
        /// <param name="body_type"></param>
        public RawBodyRequestContent(object value, bool is_serialized = false, BodyContentType body_type = BodyContentType.StringContent) : base(value) {
            BodyType = body_type;
            IsSerialized = is_serialized;
            MIMEType = "application/json";
        }
    }
}
