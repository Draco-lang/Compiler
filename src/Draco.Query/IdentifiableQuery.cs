namespace Draco.Query;

/// <summary>
/// Marker trait for query identification.
/// </summary>
public interface IIdentifiableQueryAwaiter
{
    public int Identity { get; }
}
