using System;
using System.Collections.Generic;
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
    /// The type that has to be the same as <see cref="Second"/>.
    /// </summary>
    public TypeSymbol First { get; }

    /// <summary>
    /// The type that has to be the same as <see cref="First"/>.
    /// </summary>
    public TypeSymbol Second { get; }

    public override IEnumerable<TypeVariable> TypeVariables
    {
        get
        {
            if (this.First is TypeVariable tv1) yield return tv1;
            if (this.Second is TypeVariable tv2) yield return tv2;
        }
    }

    public SameTypeConstraint(ConstraintSolver solver, TypeSymbol first, TypeSymbol second)
        : base(solver)
    {
        this.First = first;
        this.Second = second;
    }

    public override string ToString() => $"SameType({this.First}, {this.Second})";

    public override SolveState Solve(DiagnosticBag diagnostics)
    {
        // Successful unification
        if (this.Solver.Unify(this.First, this.Second))
        {
            this.Promise.Resolve(default);
            return SolveState.Solved;
        }

        // Type-mismatch
        this.Diagnostic
            .WithTemplate(TypeCheckingErrors.TypeMismatch)
            .WithFormatArgs(this.Unwrap(this.First), this.Unwrap(this.Second));
        this.Promise.Fail(default, diagnostics);
        return SolveState.Solved;
    }
}
