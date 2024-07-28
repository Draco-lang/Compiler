using System.Collections.Immutable;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver.Constraints;

/// <summary>
/// A constraint representing that a type needs to have an indexer operator.
/// </summary>
/// <param name="locator">The locator of the constraint.</param>
/// <param name="receiver">The indexed type.</param>
/// <param name="indices">The passed in index arguments.</param>
/// <param name="elementType">The element type of the indexer.</param>
/// <param name="isSetter">True, if the constraint should look for a setter. It looks for a getter otherwise.</param>
internal sealed class Indexer(
    ConstraintLocator? locator,
    TypeSymbol receiver,
    ImmutableArray<Argument> indices,
    TypeSymbol elementType,
    bool isSetter) : Constraint(locator)
{
    /// <summary>
    /// The completion source for the resolved indexer symbol.
    /// </summary>
    public SolverTaskCompletionSource<FunctionSymbol> CompletionSource { get; } = new();

    /// <summary>
    /// The indexed type.
    /// </summary>
    public TypeSymbol Receiver { get; } = receiver;

    /// <summary>
    /// The passed in index arguments.
    /// </summary>
    public ImmutableArray<Argument> Indices { get; } = indices;

    /// <summary>
    /// The element type of the indexer.
    /// </summary>
    public TypeSymbol ElementType { get; } = elementType;

    /// <summary>
    /// True, if the constraint should look for a getter. It looks for a setter otherwise.
    /// </summary>
    public bool IsGetter => !this.IsSetter;

    /// <summary>
    /// True, if the constraint should look for a setter. It looks for a getter otherwise.
    /// </summary>
    public bool IsSetter { get; } = isSetter;
}
