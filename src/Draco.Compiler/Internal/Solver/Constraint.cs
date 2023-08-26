using System;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Utility base-class for constraints.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal abstract class Constraint<TResult> : IConstraint<TResult>
{
    public IConstraintPromise<TResult> Promise { get; }
    IConstraintPromise IConstraint.Promise => this.Promise;
    public ConstraintLocator Locator { get; }

    protected Constraint(ConstraintLocator locator)
    {
        this.Promise = ConstraintPromise.Create(this);
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
