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

    public override TypeSymbol Type => this.ContainingSymbol.IndexType;
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
internal sealed class ArrayLengthGetSymbol(
    ArrayTypeSymbol containingSymbol,
    PropertySymbol propertySymbol) : FunctionSymbol, IPropertyAccessorSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => [];
    public override TypeSymbol ReturnType => this.ContainingSymbol.IndexType;
    public override bool IsStatic => false;

    public override ArrayTypeSymbol ContainingSymbol { get; } = containingSymbol;
    public PropertySymbol Property { get; } = propertySymbol;
}
