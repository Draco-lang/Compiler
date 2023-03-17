using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public OverloadConstraint(IEnumerable<FunctionSymbol> candidates, Type callSite)
    {
        this.Candidates = candidates.ToList();
        this.CallSite = callSite;
    }
}
