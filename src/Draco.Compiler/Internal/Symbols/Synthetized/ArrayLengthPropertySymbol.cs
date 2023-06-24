using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// The length property of an array.
/// </summary>
internal sealed class ArrayLengthPropertySymbol : PropertySymbol
{
    public override string Name => "Length";

    public override FunctionSymbol Getter { get; }
    public override FunctionSymbol? Setter => null;

    public override TypeSymbol Type => IntrinsicSymbols.Int32;
    public override ArrayTypeSymbol ContainingSymbol { get; }

    public override bool IsIndexer => false;
    public override bool IsStatic => false;

    public ArrayLengthPropertySymbol(ArrayTypeSymbol containingSymbol)
    {
        this.ContainingSymbol = containingSymbol;
        this.Getter = new ArrayLengthGetSymbol(containingSymbol, this);
    }
}

/// <summary>
/// The getter of array Length.
/// </summary>
internal sealed class ArrayLengthGetSymbol : FunctionSymbol, IPropertyAccessorSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;
    public override TypeSymbol ReturnType => IntrinsicSymbols.Int32;
    public override bool IsStatic => false;

    public override ArrayTypeSymbol ContainingSymbol { get; }
    public PropertySymbol Property { get; }

    public ArrayLengthGetSymbol(ArrayTypeSymbol containingSymbol, PropertySymbol propertySymbol)
    {
        this.ContainingSymbol = containingSymbol;
        this.Property = propertySymbol;
    }
}
