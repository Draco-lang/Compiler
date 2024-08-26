using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Script;

namespace Draco.Compiler.Internal.Binding;

// NOTE: Not sealed, as script module binder is derived from this
/// <summary>
/// Binds on a module level.
/// </summary>
internal class ModuleBinder : Binder
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

    protected override void ConstraintReturnType(
        SyntaxNode returnSyntax,
        BindingTask<BoundExpression> returnValue,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
        // NOTE: We ignore script module return constraints, as they have implicit returns
        // which are hopefully correct
        if (this.symbol is ScriptModuleSymbol) return;

        base.ConstraintReturnType(returnSyntax, returnValue, constraints, diagnostics);
    }

    internal override void LookupLocal(LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        foreach (var symbol in this.symbol.Members)
        {
            if (!this.IsVisible(symbol)) continue;
            if (symbol.Name != name) continue;
            if (!allowSymbol(symbol)) continue;
            if (symbol is GlobalSymbol && !flags.HasFlag(LookupFlags.AllowGlobals)) continue;
            result.Add(symbol);
        }
    }

    private bool IsVisible(Symbol symbol)
    {
        if (symbol.Visibility != Api.Semantics.Visibility.Private) return true;

        // If the module symbol of the current symbol is the same as this module, return true
        if (this.symbol == symbol.AncestorChain.OfType<ModuleSymbol>().FirstOrDefault()) return true;
        return false;
    }
}
