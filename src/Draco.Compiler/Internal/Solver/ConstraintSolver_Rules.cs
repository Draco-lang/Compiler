using System.Collections.Generic;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.Constraints;
using static Draco.Chr.Rules.RuleFactory;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private IEnumerable<Rule> ConstructRules(DiagnosticBag diagnostics) => [
            Simplification(typeof(Same))
                .Body((ConstraintStore store, Same same) =>
                {
                    for (var i = 1; i < same.Types.Length; ++i)
                    {
                        if (Unify(same.Types[0], same.Types[i])) continue;

                        // Type-mismatch
                        diagnostics.Add(Diagnostic
                            .CreateBuilder()
                            .WithLocation(same.Locator)
                            .WithTemplate(TypeCheckingErrors.TypeMismatch)
                            .WithFormatArgs(same.Types[0].Substitution, same.Types[i].Substitution)
                            .Build());
                        break;
                    }
                }),
    ];
}
