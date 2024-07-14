namespace Draco.Chr.Rules;

/// <summary>
/// Signals what a head contains for matching information.
/// </summary>
internal enum HeadContains
{
    /// <summary>
    /// Unspecified, can't optimize matching.
    /// </summary>
    Any,

    /// <summary>
    /// Type-level information, can filter based on constraint type.
    /// </summary>
    Type,

    /// <summary>
    /// Exact value, can filter based on constraint value.
    /// </summary>
    Value,
}
