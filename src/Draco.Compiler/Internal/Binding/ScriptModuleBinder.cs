using System;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Script;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds a script module.
/// </summary>
internal sealed class ScriptModuleBinder : ModuleBinder
{
    public ScriptModuleBinder(Compilation compilation, ScriptModuleSymbol symbol)
        : base(compilation, symbol)
    {
    }

    public ScriptModuleBinder(Binder parent, ScriptModuleSymbol symbol)
        : base(parent, symbol)
    {
    }

    protected override void ConstraintReturnType(
        SyntaxNode returnSyntax,
        BindingTask<BoundExpression> returnValue,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
        // NOTE: We ignore script module return constraints, as they have implicit returns
        // which are hopefully correct
    }

    internal override void LookupLocal(
        LookupResult result, string name, ref LookupFlags flags, Predicate<Symbol> allowSymbol, SyntaxNode? currentReference)
    {
        // Just add the allow globals flag, then call the base method
        // We are in a global context here, so the statements and expressions in global context
        // should be able to access the global symbols
        flags |= LookupFlags.AllowGlobals;
        base.LookupLocal(result, name, ref flags, allowSymbol, currentReference);
    }
}
