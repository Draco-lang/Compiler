using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Error;

internal class ErrorThisParameterSymbol(TypeSymbol type, FunctionSymbol containingSymbol) : ParameterSymbol
{
    public override TypeSymbol Type { get; } = type;
    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;
}
