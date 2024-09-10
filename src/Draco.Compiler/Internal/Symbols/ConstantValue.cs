namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A constant value.
/// </summary>
/// <param name="Type">The type of the argument.</param>
/// <param name="Value">The value of the argument.</param>
internal readonly record struct ConstantValue(TypeSymbol Type, object? Value);
