using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A type parameter synthetized by the compiler.
/// </summary>
internal sealed class SynthetizedTypeParameterSymbol : TypeParameterSymbol
{
    public override Symbol? ContainingSymbol => null;

    public override string Name { get; }

    public SynthetizedTypeParameterSymbol(string name)
    {
        this.Name = name;
    }
}
