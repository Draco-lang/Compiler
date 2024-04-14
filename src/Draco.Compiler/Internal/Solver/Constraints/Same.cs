using System.Collections.Immutable;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// Asserts that a set of types are all the same.
/// </summary>
/// <param name="Locator">The locator of the constraint.</param>
/// <param name="Types">The types that all should be the same.</param>
internal sealed record class Same(
    ConstraintLocator? Locator,
    ImmutableArray<TypeSymbol> Types) : ConstraintBase(Locator, TypeCheckingErrors.TypeMismatch);
