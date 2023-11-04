using System;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Utility base-class for constraints.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal abstract class Constraint<TResult> : IConstraint<TResult>
{
    public SolverTaskCompletionSource<TResult> CompletionSource { get; }
    public ConstraintLocator Locator { get; }

    protected Constraint(ConstraintSolver solver, ConstraintLocator locator)
    {
        this.CompletionSource = new(solver);
        this.Locator = locator;
    }

    public Diagnostic.Builder BuildDiagnostic(Action<Diagnostic.Builder> config)
    {
        var builder = Diagnostic.CreateBuilder();
        config(builder);
        this.Locator.Locate(builder);
        return builder;
    }

    public override abstract string ToString();
}
