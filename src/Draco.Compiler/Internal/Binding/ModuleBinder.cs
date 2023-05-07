using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds on a module level.
/// </summary>
internal sealed class ModuleBinder : Binder
{
    public override SyntaxNode? DeclaringSyntax => this.symbol.DeclaringSyntax;

    public override Symbol? ContainingSymbol => this.symbol;

    public override IEnumerable<Symbol> DeclaredSymbols => this.symbol.Members;

    private readonly ModuleSymbol symbol;

    public ModuleBinder(Compilation compilation, ModuleSymbol symbol)
        : base(compilation)
    {
        this.symbol = symbol;
    }

    public ModuleBinder(Binder parent, ModuleSymbol symbol)
        : base(parent)
    {
        this.symbol = symbol;
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        foreach (var symbol in this.symbol.Members)
        {
            if (!this.isVisible(symbol, currentReference?.Tree)) continue;
            if (symbol.Name != name) continue;
            if (!allowSymbol(symbol)) continue;
            if (symbol is GlobalSymbol && !flags.HasFlag(LookupFlags.AllowGlobals)) continue;
            result.Add(symbol);
        }
    }

    private bool isVisible(Symbol symbol, SyntaxTree? reference)
    {
        if (symbol.Visibility != Api.Semantics.VisibilityType.Private) return true;

        // If any of the files in this module are the same file as the reference, we are in the same module and we see all declared symbols
        if (this.symbol.Members.Any(x => x.DeclaringSyntax?.Tree == reference)) return true;
        return false;
    }
}
