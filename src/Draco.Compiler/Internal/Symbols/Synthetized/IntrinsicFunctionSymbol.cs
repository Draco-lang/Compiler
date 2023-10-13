using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A function intrinsic known by the compiler. This function has no implementation, as it is known by the compiler.
/// </summary>
internal sealed class IntrinsicFunctionSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters { get; }

    public override TypeSymbol ReturnType { get; }
    public override Symbol? ContainingSymbol => null;
    public override bool IsSpecialName => true;
    public override bool IsStatic => true;

    public override string Name { get; }

    public IntrinsicFunctionSymbol(string name, IEnumerable<TypeSymbol> paramTypes, TypeSymbol returnType)
    {
        this.Name = name;
        this.Parameters = paramTypes
            .Select(t => new SynthetizedParameterSymbol(this, t))
            .Cast<ParameterSymbol>()
            .ToImmutableArray();
        this.ReturnType = returnType;
    }
}
