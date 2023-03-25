using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds compiler-intrinsic symbols
/// </summary>
internal sealed class IntrinsicsBinder : Binder
{
    private static ImmutableArray<Symbol> IntrinsicSymbols { get; } = typeof(IntrinsicSymbols)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(prop => prop.PropertyType.IsAssignableTo(typeof(Symbol)))
        .Select(prop => prop.GetValue(null))
        .Cast<Symbol>()
        .ToImmutableArray();

    public override IEnumerable<Symbol> DeclaredSymbols => IntrinsicSymbols;

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
        foreach (var symbol in IntrinsicSymbols)
        {
            if (symbol.Name != name) continue;
            if (!allowSymbol(symbol)) continue;
            result.Add(symbol);
        }
    }
}
