using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// Represents a location in a source text.
/// </summary>
public abstract partial class Location
{
    /// <summary>
    /// A constant representing no location.
    /// </summary>
    public static Location None { get; } = new NullLocation();

    /// <summary>
    /// True, if this location represents no location.
    /// </summary>
    public virtual bool IsNone => false;

    /// <summary>
    /// The <see cref="Syntax.SourceText"/> the location represents.
    /// </summary>
    public virtual SourceText SourceText => SourceText.None;

    /// <summary>
    /// The range of this location.
    /// </summary>
    public virtual SyntaxRange? Range => null;
}
