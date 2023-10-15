using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A type-alias defined by the compiler.
/// </summary>
internal sealed class SynthetizedTypeAliasSymbol : TypeAliasSymbol
{
    public override string Name { get; }
    public override Symbol? ContainingSymbol => null;
    public override TypeSymbol Substitution { get; }

    public SynthetizedTypeAliasSymbol(string name, TypeSymbol substitution)
    {
        this.Name = name;
        this.Substitution = substitution;
    }

    public override void Accept(SymbolVisitor visitor) => visitor.VisitTypeAlias(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitTypeAlias(this);
    public override ISymbol ToApiSymbol() => throw new NotImplementedException();
}
