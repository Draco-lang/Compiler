using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// An attribute instance attached to a symbol.
/// </summary>
internal sealed class AttributeInstance(
    FunctionSymbol constructor,
    ImmutableArray<object?> fixedArguments,
    ImmutableDictionary<string, object?> namedArguments)
{
    /// <summary>
    /// The attribute constructor.
    /// </summary>
    public FunctionSymbol Constructor { get; } = constructor;

    /// <summary>
    /// The fixed arguments of the attribute.
    /// </summary>
    public ImmutableArray<object?> FixedArguments { get; } = fixedArguments;

    /// <summary>
    /// The named arguments of the attribute.
    /// </summary>
    public ImmutableDictionary<string, object?> NamedArguments { get; } = namedArguments;
}
