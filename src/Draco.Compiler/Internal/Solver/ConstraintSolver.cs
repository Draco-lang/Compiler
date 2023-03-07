using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves type-constraint problems for the binder.
/// </summary>
internal sealed class ConstraintSolver
{
    private readonly List<Constraint> constraints = new();

    /// <summary>
    /// Solves all constraints within the solver.
    /// </summary>
    public void Solve() => throw new NotImplementedException();
}
