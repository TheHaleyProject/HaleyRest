namespace Haley.Abstractions {
    public interface IQueryRequestContent : IRequestContent, ISetURLDecoded<IQueryRequestContent> {
        string Key { get; }
        string Value { get; }
    }
}
