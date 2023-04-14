using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;

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
    public TypeSymbol CallSite { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<FunctionSymbol> Promise { get; }

    /// <summary>
    /// The name of the overloaded set of functions.
    /// </summary>
    public string FunctionName { get; }

    public OverloadConstraint(IEnumerable<FunctionSymbol> candidates, TypeSymbol callSite, ConstraintPromise<FunctionSymbol> promise)
    {
        this.Candidates = candidates.ToList();
        this.CallSite = callSite;
        this.Promise = promise;
        this.FunctionName = this.Candidates[0].Name;
    }

    public OverloadConstraint(IEnumerable<FunctionSymbol> candidates, TypeSymbol callSite)
    {
        this.Candidates = candidates.ToList();
        this.CallSite = callSite;
        this.Promise = ConstraintPromise.Create<FunctionSymbol>(this);
        this.FunctionName = this.Candidates[0].Name;
    }
}
