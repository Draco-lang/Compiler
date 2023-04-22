using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a generic type instantiation.
/// </summary>
internal sealed class InstantiateConstraint : Constraint
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

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<TypeSymbol> Promise { get; }

    public InstantiateConstraint(
        TypeSymbol toInstantiate,
        TypeSymbol instantiated,
        ImmutableArray<TypeSymbol> arguments)
    {
        this.ToInstantiate = toInstantiate;
        this.Instantiated = instantiated;
        this.Arguments = arguments;
        this.Promise = ConstraintPromise.Create<TypeSymbol>(this);
    }
}
