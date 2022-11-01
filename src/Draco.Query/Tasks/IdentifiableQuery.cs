namespace Draco.Query.Tasks;

/// <summary>
/// Marker trait for query identification.
/// </summary>
public interface IIdentifiableQueryAwaiter
{
    public QueryIdentifier Identity { get; }
}
