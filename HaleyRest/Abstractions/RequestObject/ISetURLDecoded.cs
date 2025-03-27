namespace Haley.Abstractions {
    public interface ISetURLDecoded<T> {
        bool IsURLDecoded { get; }
        T SetAsURLDecoded();
    }
}
