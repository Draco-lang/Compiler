using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint representing that a function should be callable by a given call-site.
/// </summary>
internal sealed class OverloadConstraint : Constraint
{
    /// <summary>
    /// The candidate functions to search among.
    /// </summary>
    public IList<FunctionSymbol> Candidates { get; }

    /// <summary>
    /// The call-site to match.
    /// </summary>
    public Type CallSite { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<FunctionSymbol> Promise { get; }

    public OverloadConstraint(IEnumerable<FunctionSymbol> candidates, Type callSite)
    {
        this.Candidates = candidates.ToList();
        this.CallSite = callSite;
        this.Promise = ConstraintPromise.Create<FunctionSymbol>(this);
    }
}
