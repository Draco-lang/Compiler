using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Symbols;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint representing that a function should be callable by a given call-site.
/// </summary>
internal sealed class OverloadConstraint : Constraint
{
    /// <summary>
    /// The candidate functions to search among.
    /// </summary>
    public IList<Symbol> Candidates { get; }

    /// <summary>
    /// The call-site to match.
    /// </summary>
    public TypeSymbol CallSite { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<Symbol> Promise { get; }

    /// <summary>
    /// The name of the overloaded set of functions.
    /// </summary>
    public string FunctionName { get; }

    public OverloadConstraint(IList<Symbol> candidates, TypeSymbol callSite, ConstraintPromise<Symbol> promise)
    {
        this.Candidates = candidates;
        this.CallSite = callSite;
        this.Promise = promise;
        this.FunctionName = this.Candidates[0].Name;
    }

    public OverloadConstraint(IList<Symbol> candidates, TypeSymbol callSite)
    {
        this.Candidates = candidates;
        this.CallSite = callSite;
        this.Promise = ConstraintPromise.Create<Symbol>(this);
        this.FunctionName = this.Candidates[0].Name;
    }
}
