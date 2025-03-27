using Haley.Enums;

namespace Haley.Abstractions {
    public interface IRawBodyRequestContent : IRequestContent {
        bool IsSerialized { get; } //Should be set only once to avoid re-serializing again.
        void SetSerialized();
        BodyContentType BodyType { get; set; }

        string MIMEType { get; set; }
    }
}
