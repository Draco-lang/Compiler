using System;
using Draco.Compiler.Api.Syntax;
using ApiLocation = Draco.Compiler.Api.Diagnostics.Location;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Represents relative range in the source code.
/// </summary>
/// <param name="Offset">The offset from the element this relates to in characters.</param>
/// <param name="Width">The width of the range in characters.</param>
internal readonly record struct RelativeRange(int Offset, int Width)
{
    /// <summary>
    /// Represents an empty range.
    /// </summary>
    public static readonly RelativeRange Empty = new(Offset: 0, Width: 0);
}

/// <summary>
/// Represents a location in source code.
/// </summary>
internal abstract partial record class Location
{
    /// <summary>
    /// A singleton location representing no location.
    /// </summary>
    public static readonly Location None = new Null();

    /// <summary>
    /// Translates this <see cref="Location"/> to an <see cref="ApiLocation"/>, assuming it's relative to
    /// a <see cref="ParseNode"/>.
    /// </summary>
    /// <param name="context">The <see cref="ParseNode"/> the location is relative to.</param>
    /// <returns>The equivalent <see cref="ApiLocation"/> of <paramref name="context"/>.</returns>
    public abstract ApiLocation ToApiLocation(ParseNode? context);
}

internal abstract partial record class Location
{
    /// <summary>
    /// Represents no location.
    /// </summary>
    private sealed record class Null : Location
    {
        public override ApiLocation ToApiLocation(ParseNode? context) => ApiLocation.None;
    }
}

internal abstract partial record class Location
{
    /// <summary>
    /// Represents a <see cref="Location"/> relative to a parse-tree element.
    /// </summary>
    /// <param name="Range">The relative range compared to the tree.</param>
    public sealed record class RelativeToTree(RelativeRange Range) : Location
    {
        public override ApiLocation ToApiLocation(ParseNode? context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            var range = context.TranslateRelativeRange(this.Range);
            return new ApiLocation.InFile(range);
        }
    }
}

internal abstract partial record class Location
{
    /// <summary>
    /// Represents a <see cref="Location"/> referencing a <see cref="ParseNode"/> element.
    /// </summary>
    /// <param name="Node">The node the location refers to.</param>
    public sealed record class TreeReference(ParseNode Node) : Location
    {
        public override ApiLocation ToApiLocation(ParseNode? context) => this.Node.Location;
    }
}
