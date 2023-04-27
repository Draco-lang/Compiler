using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
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
            if (!this.isVisible(symbol, name)) continue;
            if (symbol.Name != name) continue;
            if (!allowSymbol(symbol)) continue;
            if (symbol is GlobalSymbol && !flags.HasFlag(LookupFlags.AllowGlobals)) continue;
            result.Add(symbol);
        }
    }

    private bool isVisible(Symbol symbol, string name)
    {
        if (symbol.Visibility != Api.Semantics.VisibilityType.Private) return true;
        if (this.symbol.Members.Any(x => x.DeclaringSyntax is not null && x.DeclaringSyntax.PreOrderTraverse().OfType<NameExpressionSyntax>().Count() != 0 && x.DeclaringSyntax.FindInChildren<NameExpressionSyntax>().Name.Text == name)) return true;
        return false;
    }
}
