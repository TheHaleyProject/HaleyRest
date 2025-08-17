namespace Haley.Abstractions {
    public interface IQueryRequestContent : IRequestContent, ISetURLDecoded<IQueryRequestContent> {
        string Key { get; }
        //new string Value { get; }
    }
}
