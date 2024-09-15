using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Error;

// NOTE: Not sealed on purpose, others will derive from this.
/// <summary>
/// Represents a function that has an error, like a non-existing overload.
/// </summary>
internal class ErrorFunctionSymbol : FunctionSymbol
{
    // TODO: Is this enough? Do we want to take this as a parameter too?
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => [];
    public override ImmutableArray<ParameterSymbol> Parameters { get; }
    public override TypeSymbol ReturnType => WellKnownTypes.ErrorType;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public override bool IsError => true;

    public ErrorFunctionSymbol(int parameterCount)
    {
        this.Parameters = Enumerable.Repeat(WellKnownTypes.ErrorType, parameterCount)
            .Select(t => new SynthetizedParameterSymbol(this, t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
    }
}
