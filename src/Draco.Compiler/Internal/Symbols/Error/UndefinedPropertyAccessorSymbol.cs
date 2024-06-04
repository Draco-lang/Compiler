using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a property accessor that was not defined.
/// </summary>
internal sealed class UndefinedPropertyAccessorSymbol : FunctionSymbol, IPropertyAccessorSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;
    public override TypeSymbol ReturnType => WellKnownTypes.ErrorType;
    public override bool IsError => true;
    public override bool IsStatic => false;
    public PropertySymbol Property { get; }

    public UndefinedPropertyAccessorSymbol(PropertySymbol property)
    {
        this.Property = property;
    }
}
