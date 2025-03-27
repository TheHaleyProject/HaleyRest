namespace Haley.Abstractions {
    public interface ISerializeRequest {
        bool IsSerialized { get; } //Should be set only once to avoid re-serializing again.
        void SetSerialized();
    }
}
