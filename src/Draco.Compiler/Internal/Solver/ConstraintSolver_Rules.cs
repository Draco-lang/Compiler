using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private void HandleRule(SameTypeConstraint constraint, DiagnosticBag diagnostics)
    {
        for (var i = 1; i < constraint.Types.Length; ++i)
        {
            if (!this.Unify(constraint.Types[0], constraint.Types[i]))
            {
                // Type-mismatch
                constraint.Diagnostic
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(constraint.Types[0].Substitution, constraint.Types[i].Substitution);
                constraint.Promise.Fail(default, diagnostics);
                return;
            }
        }

        // Successful unification
        constraint.Promise.Resolve(default);
    }
}
