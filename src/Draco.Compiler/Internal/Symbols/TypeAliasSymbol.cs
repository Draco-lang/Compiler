using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Alias for a type. Not a real type itself.
/// </summary>
internal abstract class TypeAliasSymbol : Symbol, IMemberSymbol
{
    public bool IsStatic => true;

    /// <summary>
    /// The type being aliased.
    /// </summary>
    public abstract TypeSymbol Substitution { get; }

    public override void Accept(SymbolVisitor visitor) => visitor.VisitTypeAlias(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitTypeAlias(this);
    public override ISymbol ToApiSymbol() => new Api.Semantics.TypeAliasSymbol(this);
}
