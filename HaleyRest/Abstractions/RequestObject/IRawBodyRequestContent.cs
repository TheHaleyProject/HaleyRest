using Haley.Enums;

namespace Haley.Abstractions {
    public interface IRawBodyRequestContent : IRequestContent {
        /// <summary>
        /// This will be shared with the HTTP Request. Can be used for sending the request with file name.
        /// </summary>
        string Title { get; }
        bool IsSerialized { get; } //Should be set only once to avoid re-serializing again.
        void SetSerialized();
        BodyContentType BodyType { get; set; }

        string MIMEType { get; set; }
    }
}
