using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Script;

/// <summary>
/// The evaluation function of a script.
/// </summary>
internal sealed class ScriptEvalFunctionSymbol(
    ScriptModuleSymbol containingSymbol,
    ScriptEntrySyntax syntax) : FunctionSymbol
{
    public override ScriptModuleSymbol ContainingSymbol => containingSymbol;
    public override ImmutableArray<ParameterSymbol> Parameters => [];
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
    public override ScriptEntrySyntax DeclaringSyntax => syntax;
    public override bool IsSpecialName => true;

    public override string Name => CompilerConstants.ScriptEntryPointName;
    public override TypeSymbol ReturnType => this.ContainingSymbol.ScriptBindings.EvalType;
    public override BoundStatement Body => this.ContainingSymbol.ScriptBindings.EvalBody;
}
