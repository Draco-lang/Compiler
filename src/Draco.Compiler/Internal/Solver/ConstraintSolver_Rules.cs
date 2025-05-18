using System;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// Tries to apply a rule to the current set of constraints.
    /// This is a fixpoint iteration method. Once it returns false, no more rules can be applied.
    /// </summary>
    /// <returns>True, if a change was made, false otherwise.</returns>
    private bool ApplyRulesOnce()
    {
        return false;
    }

    /// <summary>
    /// Fails all remaining rules in the solver.
    /// </summary>
    private void FailRemainingRules()
    {
        // TODO
        throw new NotImplementedException();
    }
}
