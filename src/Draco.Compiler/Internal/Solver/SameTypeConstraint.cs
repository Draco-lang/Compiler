using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint asserting that two types have to be exactly the same.
/// </summary>
internal sealed class SameTypeConstraint : Constraint<Unit>
{
    /// <summary>
    /// The types that all should be the same.
    /// </summary>
    public ImmutableArray<TypeSymbol> Types { get; }

    public override IEnumerable<TypeVariable> TypeVariables =>
        this.Types.OfType<TypeVariable>();

    public SameTypeConstraint(ConstraintSolver solver, ImmutableArray<TypeSymbol> types)
        : base(solver)
    {
        this.Types = types;
    }

    public override string ToString() => $"SameType({string.Join(", ", this.Types)})";

    public override SolveState Solve(DiagnosticBag diagnostics)
    {
        for (var i = 1; i < this.Types.Length; ++i)
        {
            if (!this.Solver.Unify(this.Types[0], this.Types[i]))
            {
                // Type-mismatch
                this.Diagnostic
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(this.Unwrap(this.Types[0]), this.Unwrap(this.Types[i]));
                this.Promise.Fail(default, diagnostics);
                return SolveState.Solved;
            }
        }

        // Successful unification
        this.Promise.Resolve(default);
        return SolveState.Solved;
    }
}
