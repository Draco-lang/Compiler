using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a delegate selection from a function group.
/// </summary>
internal sealed class DelegateConstraint : Constraint<FunctionSymbol>
{
    /// <summary>
    /// The functions to select from.
    /// </summary>
    public ImmutableArray<FunctionSymbol> Functions { get; }

    /// <summary>
    /// The type of the function.
    /// </summary>
    public TypeSymbol FunctionType { get; }

    public DelegateConstraint(
        ConstraintSolver solver,
        ImmutableArray<FunctionSymbol> functions,
        TypeSymbol functionType)
        : base(solver)
    {
        this.Functions = functions;
        this.FunctionType = functionType;
    }

    public override string ToString() =>
        $"Delegate(functions: [{string.Join(", ", this.Functions)}]) => {this.FunctionType}";

    public override void FailSilently()
    {
        // TODO
    }

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        // TODO
        throw new NotImplementedException();
    }
}
