using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A global constructor for arrays.
/// </summary>
internal sealed class ArrayConstructorSymbol : SynthetizedFunctionSymbol
{
    public override string Name => this.Rank switch
    {
        1 => "Array",
        int n => $"Array{n}D",
    };

    public override ImmutableArray<TypeParameterSymbol> GenericParameters => ImmutableArray.Create(this.ElementType);

    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType =>
        InterlockedUtils.InitializeNull(ref this.returnType, this.BuildReturnType);
    private TypeSymbol? returnType;

    /// <summary>
    /// The rank of the array to construct.
    /// </summary>
    public int Rank => this.genericArrayType.Rank;

    /// <summary>
    /// The array element type.
    /// </summary>
    public TypeParameterSymbol ElementType =>
        InterlockedUtils.InitializeNull(ref this.elementType, this.BuildElementType);
    private TypeParameterSymbol? elementType;

    public override BoundStatement Body =>
        InterlockedUtils.InitializeNull(ref this.body, this.BuildBody);
    private BoundStatement? body;

    private readonly ArrayTypeSymbol genericArrayType;

    public ArrayConstructorSymbol(ArrayTypeSymbol genericArrayType)
    {
        this.genericArrayType = genericArrayType;
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.Rank switch
    {
        1 => ImmutableArray.Create(new SynthetizedParameterSymbol(this, "capacity", this.genericArrayType.IndexType) as ParameterSymbol),
        int n => Enumerable
            .Range(1, n)
            .Select(i => new SynthetizedParameterSymbol(this, $"capacity{i}", this.genericArrayType.IndexType) as ParameterSymbol)
            .ToImmutableArray(),
    };

    private TypeSymbol BuildReturnType() => this.genericArrayType.GenericInstantiate(this.ElementType);

    private TypeParameterSymbol BuildElementType() => new SynthetizedTypeParameterSymbol(this, "T");

    private BoundStatement BuildBody() => ExpressionStatement(ReturnExpression(
        value: ArrayCreationExpression(
            elementType: this.ElementType,
            sizes: this.Parameters
                .Select(ParameterExpression)
                .Cast<BoundExpression>()
                .ToImmutableArray(),
            type: this.ReturnType)));
}
