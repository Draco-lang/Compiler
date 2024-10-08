namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a property accessor with an error - for example, it was not defined.
/// </summary>
internal sealed class ErrorPropertyAccessorSymbol(PropertySymbol property, int parameterCount = 0)
    : ErrorFunctionSymbol(parameterCount), IPropertyAccessorSymbol
{
    public override bool IsStatic => true;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
    public PropertySymbol Property { get; } = property;
}
