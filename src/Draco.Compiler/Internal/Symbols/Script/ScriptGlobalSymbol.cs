using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Script;

internal sealed class ScriptGlobalSymbol : GlobalSymbol, ISourceSymbol
{
    public override TypeSymbol Type => throw new System.NotImplementedException();

    public override bool IsMutable => throw new System.NotImplementedException();

    public void Bind(IBinderProvider binderProvider) => throw new System.NotImplementedException();
}
