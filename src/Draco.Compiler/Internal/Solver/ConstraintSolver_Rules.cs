using System.Collections.Generic;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.Constraints;
using Draco.Compiler.Internal.Symbols;
using static Draco.Chr.Rules.RuleFactory;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private static IEnumerable<Rule> ConstructRules(DiagnosticBag diagnostics) => [
        // Trivial same-type constraint, unify all
        Simplification(typeof(Same))
            .Body((ConstraintStore store, Same same) =>
            {
                for (var i = 1; i < same.Types.Length; ++i)
                {
                    if (Unify(same.Types[0], same.Types[i])) continue;
                    // Type-mismatch
                    same
                        .ReportDiagnostic(diagnostics, builder => builder
                        .WithFormatArgs(same.Types[0].Substitution, same.Types[i].Substitution));
                    break;
                }
            }),
        // Assignable can be resolved directly, if both types are ground-types
        Simplification(typeof(Assignable))
            .Guard((Assignable assignable) => assignable.TargetType.IsGroundType
                                           && assignable.AssignedType.IsGroundType)
            .Body((ConstraintStore store, Assignable assignable) =>
            {
                if (SymbolEqualityComparer.Default.IsBaseOf(assignable.TargetType, assignable.AssignedType))
                {
                    // Ok
                    return;
                }
                // Error
                assignable
                    .ReportDiagnostic(diagnostics, diag => diag
                    .WithFormatArgs(assignable.TargetType.Substitution, assignable.AssignedType.Substitution));
            }),
    ];
}
