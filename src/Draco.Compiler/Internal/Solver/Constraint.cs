using System;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;
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
    public virtual bool Silent => false;

    protected Constraint(ConstraintLocator locator)
    {
        this.CompletionSource = new();
        this.Locator = locator;
    }

    private Diagnostic.Builder ConfigureDiagnostic(Action<Diagnostic.Builder> config)
    {
        var builder = Diagnostic.CreateBuilder();
        config(builder);
        this.Locator.Locate(builder);
        return builder;
    }

    public void ReportDiagnostic(DiagnosticBag? diagnostics, Action<Diagnostic.Builder> config)
    {
        if (this.Silent) return;
        if (diagnostics is null) return;

        var builder = this.ConfigureDiagnostic(config);
        diagnostics.Add(builder.Build());
    }

    public override abstract string ToString();
}
