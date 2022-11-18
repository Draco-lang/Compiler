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
}

public abstract partial class Location
{
    internal sealed class Null : Location
    {
        public override bool IsNone => true;

        public override string ToString() => "<no location>";
    }

    internal sealed class Tree : Location
    {
        private readonly Range range;

        public Tree(Range range)
        {
            this.range = range;
        }

        public override string ToString()
        {
            var position = this.range.Start;
            return $"at line {position.Line + 1}, character {position.Column + 1}";
        }
    }
}
