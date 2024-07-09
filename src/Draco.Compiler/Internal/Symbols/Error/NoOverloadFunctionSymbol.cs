using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a non-existing function for an overload resolution that failed.
/// </summary>
internal sealed class NoOverloadFunctionSymbol : FunctionSymbol
{
    // TODO: Is this enough? Do we want to take this as a parameter too?
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => [];
    public override ImmutableArray<ParameterSymbol> Parameters { get; }
    public override TypeSymbol ReturnType => WellKnownTypes.ErrorType;

    public override bool IsError => true;

    public NoOverloadFunctionSymbol(int parameterCount)
    {
        this.Parameters = Enumerable.Repeat(WellKnownTypes.ErrorType, parameterCount)
            .Select(t => new SynthetizedParameterSymbol(this, t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
    }
}
