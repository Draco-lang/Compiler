using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A built-in primitive.
/// </summary>
internal sealed class PrimitiveTypeSymbol : TypeSymbol
{
    public override Symbol? ContainingSymbol => null;

    public override string Name { get; }

    public PrimitiveTypeSymbol(string name)
    {
        this.Name = name;
    }

    public override string ToString() => this.Name;
}
