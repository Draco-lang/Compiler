using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a generic type instantiation.
/// </summary>
internal sealed class InstantiateConstraint : Constraint<TypeSymbol>
{
    /// <summary>
    /// The type to instantiate.
    /// </summary>
    public TypeSymbol ToInstantiate { get; }

    /// <summary>
    /// The instantiated type.
    /// </summary>
    public TypeSymbol Instantiated { get; }

    /// <summary>
    /// The arguments the type gets instantiated with.
    /// </summary>
    public ImmutableArray<TypeSymbol> Arguments { get; }

    public InstantiateConstraint(
        ConstraintSolver solver,
        TypeSymbol toInstantiate,
        TypeSymbol instantiated,
        ImmutableArray<TypeSymbol> arguments)
        : base(solver)
    {
        this.ToInstantiate = toInstantiate;
        this.Instantiated = instantiated;
        this.Arguments = arguments;
    }

    public override string ToString() => throw new NotImplementedException();

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics) => throw new NotImplementedException();
}
