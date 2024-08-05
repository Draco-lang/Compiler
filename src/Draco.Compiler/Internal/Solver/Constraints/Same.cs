using System.Collections.Immutable;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Asserts that a set of types are all the same.
/// </summary>
/// <param name="locator">The locator of the constraint.</param>
/// <param name="types">The types that all should be the same.</param>
internal sealed class Same(
    ConstraintLocator? locator,
    ImmutableArray<TypeSymbol> types) : Constraint(locator, TypeCheckingErrors.TypeMismatch)
{
    /// <summary>
    /// The types that all should be the same.
    /// </summary>
    public ImmutableArray<TypeSymbol> Types { get; } = types;
}
