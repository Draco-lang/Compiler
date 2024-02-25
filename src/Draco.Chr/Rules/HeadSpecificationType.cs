namespace Draco.Chr.Rules;

/// <summary>
/// The way set of heads were specified to match.
/// </summary>
internal enum HeadSpecificationType
{
    /// <summary>
    /// Only the number of heads was specified.
    /// </summary>
    SizeSpecified,

    /// <summary>
    /// The types of the heads were specified.
    /// </summary>
    TypesSpecified,

    /// <summary>
    /// The heads were specified individually.
    /// </summary>
    ComplexDefinition,
}
