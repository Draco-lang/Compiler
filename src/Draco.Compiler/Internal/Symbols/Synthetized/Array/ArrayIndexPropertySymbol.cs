using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Synthetized.Array;

/// <summary>
/// The indexer property of arrays.
/// </summary>
internal sealed class ArrayIndexPropertySymbol : PropertySymbol
{
    public override FunctionSymbol Getter { get; }
    public override FunctionSymbol Setter { get; }
    public override TypeSymbol Type => this.ContainingSymbol;
    public override ArrayTypeSymbol ContainingSymbol { get; }

    public override bool IsIndexer => true;
    public override bool IsStatic => false;
    public override bool IsExplicitImplementation => false;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public ArrayIndexPropertySymbol(ArrayTypeSymbol containingSymbol)
    {
        this.ContainingSymbol = containingSymbol;
        this.Getter = new ArrayIndexGetSymbol(containingSymbol, this);
        this.Setter = new ArrayIndexSetSymbol(containingSymbol, this);
    }
}

/// <summary>
/// The index getter of arrays.
/// </summary>
internal sealed class ArrayIndexGetSymbol(
    ArrayTypeSymbol containingSymbol,
    PropertySymbol propertySymbol) : FunctionSymbol, IPropertyAccessorSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => this.ContainingSymbol.ElementType;
    public override bool IsStatic => false;
    public override string Name => "Array_get";
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public override ArrayTypeSymbol ContainingSymbol { get; } = containingSymbol;
    public PropertySymbol Property { get; } = propertySymbol;

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.ContainingSymbol.Rank == 1
        ? [new SynthetizedParameterSymbol(this, "index", this.ContainingSymbol.IndexType) as ParameterSymbol]
        : Enumerable
            .Range(1, this.ContainingSymbol.Rank)
            .Select(i => new SynthetizedParameterSymbol(this, $"index{i}", this.ContainingSymbol.IndexType) as ParameterSymbol)
            .ToImmutableArray();
}

/// <summary>
/// The index setter of arrays.
/// </summary>
internal sealed class ArrayIndexSetSymbol(
    ArrayTypeSymbol containingSymbol,
    PropertySymbol propertySymbol) : FunctionSymbol, IPropertyAccessorSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => this.ContainingSymbol.ElementType;
    public override bool IsStatic => false;
    public override string Name => "Array_set";
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public override ArrayTypeSymbol ContainingSymbol { get; } = containingSymbol;
    public PropertySymbol Property { get; } = propertySymbol;

    private ImmutableArray<ParameterSymbol> BuildParameters()
    {
        var result = ImmutableArray.CreateBuilder<ParameterSymbol>();
        if (this.ContainingSymbol.Rank == 1)
        {
            result.Add(new SynthetizedParameterSymbol(this, "index", this.ContainingSymbol.IndexType));
        }
        else
        {
            result.AddRange(Enumerable
                .Range(1, this.ContainingSymbol.Rank)
                .Select(i => new SynthetizedParameterSymbol(this, $"index{i}", this.ContainingSymbol.IndexType)));
        }
        // Add the value
        result.Add(new SynthetizedParameterSymbol(this, "value", this.ContainingSymbol.ElementType));
        return result.ToImmutable();
    }
}
