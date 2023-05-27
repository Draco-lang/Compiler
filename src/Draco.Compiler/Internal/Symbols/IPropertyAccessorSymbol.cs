namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an property accessor symbol, is intended to use while also inheriting <see cref="FunctionSymbol"/>.
/// </summary>
internal interface IPropertyAccessorSymbol
{
    /// <summary>
    /// The property that uses this accessor.
    /// </summary>
    public PropertySymbol Property { get; }
}
