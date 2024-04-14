using System.Collections.Generic;
using System.Linq;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.Constraints;
using Draco.Compiler.Internal.Symbols;
using static Draco.Chr.Rules.RuleFactory;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private IEnumerable<Rule> ConstructRules(DiagnosticBag diagnostics) => [
        // Trivial same-type constraint, unify all
        Simplification(typeof(Same))
            .Body((ConstraintStore store, Same same) =>
            {
                for (var i = 1; i < same.Types.Length; ++i)
                {
                    if (Unify(same.Types[0], same.Types[i])) continue;

                    // Type-mismatch
                    diagnostics.Add(Diagnostic
                        .CreateBuilder()
                        .WithTemplate(TypeCheckingErrors.TypeMismatch)
                        .WithLocation(same.Locator)
                        .WithFormatArgs(same.Types[0].Substitution, same.Types[i].Substitution)
                        .Build());
                    break;
                }
            }),

        // If all types are ground-types, common-type constraints are trivial
        Simplification(typeof(CommonAncestor))
            .Guard((CommonAncestor common) => common.AlternativeTypes.All(t => t.IsGroundType))
            .Body((ConstraintStore store, CommonAncestor common) =>
            {
                foreach (var type in common.AlternativeTypes)
                {
                    if (!common.AlternativeTypes.All(t => SymbolEqualityComparer.Default.IsBaseOf(type, t))) continue;

                    // Found a good common type
                    UnifyAsserted(common.CommonType, type);
                    return;
                }

                // No common type found
                diagnostics.Add(Diagnostic
                    .CreateBuilder()
                    .WithTemplate(TypeCheckingErrors.NoCommonType)
                    .WithLocation(common.Locator)
                    .WithFormatArgs(string.Join(", ", common.AlternativeTypes))
                    .Build());
                // Stop cascading uninferred type
                UnifyAsserted(common.CommonType, WellKnownTypes.ErrorType);
            }),
    ];
}
