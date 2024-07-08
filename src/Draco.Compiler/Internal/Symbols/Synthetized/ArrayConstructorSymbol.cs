using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A global constructor for arrays.
/// </summary>
internal sealed class ArrayConstructorSymbol(ArrayTypeSymbol genericArrayType) : FunctionSymbol
{
    public override string Name => this.Rank switch
    {
        1 => "Array",
        int n => $"Array{n}D",
    };

    public override ImmutableArray<TypeParameterSymbol> GenericParameters => [this.ElementType];

    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType =>
        LazyInitializer.EnsureInitialized(ref this.returnType, this.BuildReturnType);
    private TypeSymbol? returnType;

    /// <summary>
    /// The rank of the array to construct.
    /// </summary>
    public int Rank => genericArrayType.Rank;

    /// <summary>
    /// The array element type.
    /// </summary>
    public TypeParameterSymbol ElementType =>
        LazyInitializer.EnsureInitialized(ref this.elementType, this.BuildElementType);
    private TypeParameterSymbol? elementType;

    public override CodegenDelegate Codegen => (codegen, targetType, operands) =>
    {
        var target = codegen.DefineRegister(targetType);
        var elementType = targetType.Substitution.GenericArguments[0];
        codegen.Write(NewArray(target, elementType, operands));
        return target;
    };

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.Rank switch
    {
        1 => [new SynthetizedParameterSymbol(this, "capacity", genericArrayType.IndexType) as ParameterSymbol],
        int n => Enumerable
            .Range(1, n)
            .Select(i => new SynthetizedParameterSymbol(this, $"capacity{i}", genericArrayType.IndexType) as ParameterSymbol)
            .ToImmutableArray(),
    };

    private TypeSymbol BuildReturnType() => genericArrayType.GenericInstantiate(this.ElementType);

    private TypeParameterSymbol BuildElementType() => new SynthetizedTypeParameterSymbol(this, "T");
}
