using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public static readonly Location None = new Null();

    /// <summary>
    /// True, if this location represents no location.
    /// </summary>
    public virtual bool IsNone => false;

    /// <summary>
    /// The range of this location.
    /// </summary>
    public virtual Range? Range => null;
}

public abstract partial class Location
{
    /// <summary>
    /// No location.
    /// </summary>
    internal sealed class Null : Location
    {
        public override bool IsNone => true;

        public override string ToString() => "<no location>";
    }
}

public abstract partial class Location
{
    // TODO: Eventually we'll need to store a file here

    /// <summary>
    /// A location in file.
    /// </summary>
    internal sealed class InFile : Location
    {
        public override Range? Range { get; }

        public InFile(Range range)
        {
            this.Range = range;
        }

        public override string ToString()
        {
            var position = this.Range!.Value.Start;
            return $"at line {position.Line + 1}, character {position.Column + 1}";
        }
    }
}
