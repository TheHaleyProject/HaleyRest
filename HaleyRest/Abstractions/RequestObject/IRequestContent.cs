namespace Haley.Abstractions {
    public interface IRequestContent {
        string Id { get; }
        object Value { get;  }
        string Description { get; set; }
        void UpdateValue(object value);
    }
}
