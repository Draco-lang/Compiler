namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an property accessor symbol, for example get, set and init accessors in c#. This interface is intended to be used while also inheriting <see cref="FunctionSymbol"/>.
/// </summary>
internal interface IPropertyAccessorSymbol
{
    /// <summary>
    /// The property that uses this accessor.
    /// </summary>
    public PropertySymbol Property { get; }
}
