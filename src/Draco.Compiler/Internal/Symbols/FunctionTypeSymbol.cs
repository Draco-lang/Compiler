using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a function type.
/// </summary>
internal sealed class FunctionTypeSymbol : TypeSymbol
{
    /// <summary>
    /// The parameters of the function.
    /// </summary>
    public ImmutableArray<ParameterSymbol> Parameters { get; }

    /// <summary>
    /// The return type of the function.
    /// </summary>
    public TypeSymbol ReturnType { get; }

    public override bool IsGround => this.Parameters.All(p => p.Type.IsGround) && this.ReturnType.IsGround;

    public override Symbol? ContainingSymbol => null;

    public FunctionTypeSymbol(ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
    {
        this.Parameters = parameters;
        this.ReturnType = returnType;
    }

    public override string ToString() =>
        $"({string.Join(", ", this.Parameters)}) -> {this.ReturnType}";
}
