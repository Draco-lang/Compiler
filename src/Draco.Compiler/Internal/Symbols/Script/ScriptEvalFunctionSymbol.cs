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
    ScriptEntrySyntax syntax) : FunctionSymbol, ISourceSymbol
{
    public override ScriptModuleSymbol ContainingSymbol => containingSymbol;
    public override ImmutableArray<ParameterSymbol> Parameters => [];
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public override string Name => CompilerConstants.ScriptEntryPointName;
    public override TypeSymbol ReturnType => throw new System.NotImplementedException();
    public override BoundStatement Body => throw new System.NotImplementedException();

    public void Bind(IBinderProvider binderProvider) => throw new System.NotImplementedException();
}
