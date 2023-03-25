using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A parameter constructed by the compiler.
/// </summary>
internal sealed class SynthetizedParameterSymbol : ParameterSymbol
{
    public override Type Type { get; }

    public override Symbol? ContainingSymbol => null;

    public SynthetizedParameterSymbol(Type type)
    {
        this.Type = type;
    }
}
