using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A global constructor for arrays.
/// </summary>
internal sealed class ArrayConstructorSymbol : SynthetizedFunctionSymbol
{
    public override string Name => "Array";

    public override ImmutableArray<TypeParameterSymbol> GenericParameters => ImmutableArray.Create(this.ElementType);

    public override ImmutableArray<ParameterSymbol> Parameters =>
        this.parameters.IsDefault ? (this.parameters = this.BuildParameters()) : this.parameters;
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => this.returnType ??= this.BuildReturnType();
    private TypeSymbol? returnType;

    /// <summary>
    /// The rank of the array to construct.
    /// </summary>
    public int Rank { get; }

    /// <summary>
    /// The array element type.
    /// </summary>
    public TypeParameterSymbol ElementType => this.elementType ??= this.BuildElementType();
    private TypeParameterSymbol? elementType;

    public override BoundStatement Body => this.body ??= this.BuildBody();
    private BoundStatement? body;

    public ArrayConstructorSymbol(int rank)
    {
        this.Rank = rank;
    }

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.Rank switch
    {
        1 => ImmutableArray.Create(new SynthetizedParameterSymbol(this, "capacity", IntrinsicSymbols.Int32) as ParameterSymbol),
        int n => Enumerable
            .Range(1, n)
            .Select(i => new SynthetizedParameterSymbol(this, $"capacity{i}", IntrinsicSymbols.Int32) as ParameterSymbol)
            .ToImmutableArray(),
    };

    private TypeSymbol BuildReturnType() => this.Rank switch
    {
        1 => IntrinsicSymbols.Array.GenericInstantiate(this.ElementType),
        int n => new ArrayTypeSymbol(n).GenericInstantiate(this.ElementType),
    };

    private TypeParameterSymbol BuildElementType() => new SynthetizedTypeParameterSymbol(this, "T");

    private BoundStatement BuildBody() => ExpressionStatement(ReturnExpression(
        value: ArrayCreationExpression(
            elementType: this.ElementType,
            sizes: this.Parameters
                .Select(ParameterExpression)
                .Cast<BoundExpression>()
                .ToImmutableArray())));
}
