using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a non-existing function for an overload resolution that failed.
/// </summary>
internal sealed class NoOverloadFunctionSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters { get; }
    public override Types.Type ReturnType => Types.Intrinsics.Error;

    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override Api.Semantics.ISymbol ToApiSymbol() => throw new System.NotImplementedException();

    public NoOverloadFunctionSymbol(int parameterCount)
    {
        this.Parameters = Enumerable.Repeat(Types.Intrinsics.Error, parameterCount)
            .Select(t => new SynthetizedParameterSymbol(t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
    }
}
