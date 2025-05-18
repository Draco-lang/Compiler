using System;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.Constraints;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// Tries to apply a rule to the current set of constraints.
    /// This is a fixpoint iteration method. Once it returns false, no more rules can be applied.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>True, if a change was made, false otherwise.</returns>
    private bool ApplyRulesOnce(DiagnosticBag diagnostics)
    {
        // Trivial same-type constraint, unify all
        if (this.constraintStore.TryRemove<Same>(out var same))
        {
            for (var i = 1; i < same.Types.Length; ++i)
            {
                if (Unify(same.Types[0], same.Types[i])) continue;
                // Type-mismatch
                same.ReportDiagnostic(diagnostics, builder => builder
                    .WithFormatArgs(same.Types[0].Substitution, same.Types[i].Substitution));
                break;
            }
            return true;
        }

        // Assignable can be resolved directly, if both types are ground-types
        if (this.constraintStore.TryRemove<Assignable>(out var assignable, assignable => assignable.TargetType.IsGroundType
                                                                                      && assignable.AssignedType.IsGroundType))
        {
            var targetType = assignable.TargetType;
            var assignedType = assignable.AssignedType;
            if (!SymbolEqualityComparer.Default.IsBaseOf(targetType, assignedType))
            {
                // Error
                assignable.ReportDiagnostic(diagnostics, diag => diag
                    .WithFormatArgs(targetType, assignedType));
            }
            return true;
        }

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
