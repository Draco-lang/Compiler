using System;
using System.Collections.Generic;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds compiler-intrinsic symbols
/// </summary>
internal sealed class IntrinsicsBinder : Binder
{
    public override IEnumerable<Symbol> DeclaredSymbols => this.IntrinsicSymbols.AllSymbols;

    public IntrinsicsBinder(Compilation compilation)
        : base(compilation)
    {
    }

    public IntrinsicsBinder(Binder parent)
        : base(parent)
    {
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        foreach (var symbol in this.DeclaredSymbols)
        {
            if (symbol.Name != name) continue;
            if (!allowSymbol(symbol)) continue;
            result.Add(symbol);
        }
    }
}
