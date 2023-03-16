using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an illegal, in-source value reference.
/// </summary>
internal sealed class ErrorValueSymbol : Symbol
{
    public override bool IsError => true;
    public override Symbol? ContainingSymbol => throw new NotImplementedException();

    public override string Name { get; }

    public ErrorValueSymbol(string name)
    {
        this.Name = name;
    }

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.AnySymbol(this);
}
