using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type parameter in a generic context.
/// </summary>
internal abstract class TypeParameterSymbol : TypeSymbol
{
    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.TypeParameterSymbol(this);
}
