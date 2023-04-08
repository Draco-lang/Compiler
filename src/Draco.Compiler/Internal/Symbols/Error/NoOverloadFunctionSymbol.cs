using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a non-existing function for an overload resolution that failed.
/// </summary>
internal sealed class NoOverloadFunctionSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters { get; }
    public override Type ReturnType => IntrinsicTypes.Error;

    public override Symbol? ContainingSymbol => null;

    public NoOverloadFunctionSymbol(int parameterCount)
    {
        this.Parameters = Enumerable.Repeat(IntrinsicTypes.Error, parameterCount)
            .Select(t => new SynthetizedParameterSymbol(t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
    }
}
