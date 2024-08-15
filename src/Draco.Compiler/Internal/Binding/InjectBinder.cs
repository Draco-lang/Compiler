using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds a custom set of injected symbols.
/// </summary>
internal sealed class InjectBinder : Binder
{
    public override IEnumerable<Symbol> DeclaredSymbols =>
        InterlockedUtils.InitializeDefault(ref this.delcaredSymbols, this.BuildDeclaredSymbols);
    private ImmutableArray<Symbol> delcaredSymbols;

    private readonly ImmutableArray<(string Name, string FullName)> injectedSymbols;

    public InjectBinder(Compilation compilation, ImmutableArray<(string Name, string FullName)> injectedSymbols)
        : base(compilation)
    {
        this.injectedSymbols = injectedSymbols;
    }

    public InjectBinder(Binder parent, ImmutableArray<(string Name, string FullName)> injectedSymbols)
        : base(parent)
    {
        this.injectedSymbols = injectedSymbols;
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

    private ImmutableArray<Symbol> BuildDeclaredSymbols() => this.injectedSymbols
        .SelectMany(s =>
        {
            var parts = s.FullName.Split('.').ToImmutableArray();
            var symbol = this.Compilation.RootModule.Lookup(parts);
            return symbol;
        })
        .ToImmutableArray();
}
