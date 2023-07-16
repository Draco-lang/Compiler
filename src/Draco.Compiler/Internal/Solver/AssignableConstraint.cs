using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Constraint asserting that one type is assignable to other type.
/// </summary>
internal class AssignableConstraint : Constraint<Unit>
{
    /// <summary>
    /// The type being assigned to.
    /// </summary>
    public TypeSymbol TargetType { get; }

    /// <summary>
    /// The type assigned.
    /// </summary>
    public TypeSymbol AssignedType { get; }

    public AssignableConstraint(ConstraintSolver solver, TypeSymbol targetType, TypeSymbol assignedType)
        : base(solver)
    {
        this.TargetType = targetType;
        this.AssignedType = assignedType;
    }

    public override string ToString() => $"Assign({this.TargetType}, {this.AssignedType})";

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        if (this.Solver.Unify(this.TargetType, this.AssignedType) || IsBase(this.TargetType, this.AssignedType))
        {
            // Successful unification
            this.Promise.Resolve(default);
            yield return SolveState.Solved;
        }

        // Type-mismatch
        this.Diagnostic
            .WithTemplate(TypeCheckingErrors.TypeMismatch)
            .WithFormatArgs(this.TargetType.Substitution, this.AssignedType.Substitution);
        this.Promise.Fail(default, diagnostics);
        yield return SolveState.Solved;
    }
}
