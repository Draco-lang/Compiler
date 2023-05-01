using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols.Generic;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type parameter in a generic context.
/// </summary>
internal abstract class TypeParameterSymbol : TypeSymbol
{
    public override TypeSymbol GenericInstantiate(GenericContext context) => context.TryGetValue(this, out var type)
        ? type
        : this;

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.TypeParameterSymbol(this);

    public override string ToString() => this.Name;

    public override void Accept(SymbolVisitor visitor) => visitor.VisitTypeParameter(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitTypeParameter(this);
}
