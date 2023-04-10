using System.Collections.Immutable;
using System.Diagnostics;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

internal sealed class SynthetizedArrayFunctionSymbol : FunctionSymbol
{
    public SynthetizedArrayFunctionSymbol(ArrayTypeSymbol arrayType, FunctionKind kind)
    {
        this.ArrayType = arrayType;
        this.Kind = kind;
    }

    public ArrayTypeSymbol ArrayType { get; }

    public FunctionKind Kind { get; }

    private ImmutableArray<ParameterSymbol>? parameters;

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.Kind switch
    {
        FunctionKind.Constructor => ImmutableArray.Create<ParameterSymbol>(new SynthetizedParameterSymbol(IntrinsicSymbols.Int32)),
        FunctionKind.Get => ImmutableArray.Create<ParameterSymbol>(new SynthetizedParameterSymbol(IntrinsicSymbols.Int32)),
        FunctionKind.Set => ImmutableArray.Create<ParameterSymbol>(new SynthetizedParameterSymbol(IntrinsicSymbols.Int32), new SynthetizedParameterSymbol(this.ArrayType.ElementType)),
        _ => throw new UnreachableException()
    };

    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.BuildParameters();

    public override TypeSymbol ReturnType => this.Kind switch
    {
        FunctionKind.Constructor => this.ArrayType,
        FunctionKind.Get => this.ArrayType.ElementType,
        FunctionKind.Set => IntrinsicSymbols.Unit,
        _ => throw new UnreachableException()
    };

    public override Symbol? ContainingSymbol => this.ArrayType;

    public enum FunctionKind
    {
        Constructor,
        Get,
        Set,
        // Address
    }
}
