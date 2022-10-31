namespace Draco.Query;

/// <summary>
/// Marker trait for query identification.
/// </summary>
public interface IIdentifiableQueryAwaiter
{
    string Identity { get; }
}
