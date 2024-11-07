using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds an in-source defined class.
/// </summary>
internal sealed class ClassBinder(Binder parent, TypeSymbol symbol) : Binder(parent)
{
    public override TypeSymbol ContainingSymbol => this.symbol;
    public override SyntaxNode? DeclaringSyntax => this.symbol.DeclaringSyntax;

    private readonly TypeSymbol symbol = symbol;

    public override IEnumerable<Symbol> DeclaredSymbols => this.symbol.Members
        .Concat(this.symbol.GenericParameters);

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        foreach (var typeParam in this.symbol.GenericParameters)
        {
            if (typeParam.Name != name) continue;
            if (!allowSymbol(typeParam)) continue;
            result.Add(typeParam);
            break;
        }

        foreach (var member in this.symbol.Members)
        {
            if (member.Name != name) continue;
            if (!allowSymbol(member)) continue;
            result.Add(member);
            // NOTE: We don't break here, because there are potentially multiple overloads
        }
    }
}