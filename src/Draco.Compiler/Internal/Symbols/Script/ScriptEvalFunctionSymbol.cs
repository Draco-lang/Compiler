using System.Collections.Immutable;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Script;

internal sealed class ScriptEvalFunctionSymbol : FunctionSymbol, ISourceSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => throw new System.NotImplementedException();

    public override TypeSymbol ReturnType => throw new System.NotImplementedException();

    public void Bind(IBinderProvider binderProvider) => throw new System.NotImplementedException();
}
