using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Factory methods for <see cref="ConstraintPromise{TResult}"/>.
/// </summary>
internal static class ConstraintPromise
{
    public static ConstraintPromise<TResult> Create<TResult>(Constraint constraint) =>
        new ResolvableConstraintPromise<TResult>(constraint.Diagnostic);

    public static ConstraintPromise<TResult> FromResult<TResult>(Constraint constraint, TResult result)
    {
        var promise = Create<TResult>(constraint);
        promise.Resolve(result);
        return promise;
    }

    public static ConstraintPromise<TResult> FromResult<TResult>(TResult result) =>
        new ResolvedConstraintPromise<TResult>(result);

    public static ConstraintPromise<TNewResult> Map<TOldResult, TNewResult>(
        this ConstraintPromise<TOldResult> promise,
        Func<TOldResult, TNewResult> mapFunc) =>
        new MappedConstraintPromise<TOldResult, TNewResult>(promise, mapFunc);
}

/// <summary>
/// Represents a promise to a <see cref="Solver.Constraint"/> being solved.
/// </summary>
/// <typeparam name="TResult">The result type of the promise.</typeparam>
internal abstract class ConstraintPromise<TResult>
{
    /// <summary>
    /// True, if this promise is resolved.
    /// </summary>
    public abstract bool IsResolved { get; }

    /// <summary>
    /// The result of the promise.
    /// </summary>
    public abstract TResult Result { get; }

    /// <summary>
    /// The builder for a <see cref="Diagnostics.Diagnostic"/>.
    /// </summary>
    public abstract Diagnostic.Builder Diagnostic { get; }

    /// <summary>
    /// Resolves this promise with the given result.
    /// </summary>
    /// <param name="result">The result value to resolve with.</param>
    public abstract void Resolve(TResult result);

    /// <summary>
    /// Configures the diagnostic messages for the constraint of this promise in case it fails.
    /// </summary>
    /// <param name="configure">The configuration function.</param>
    /// <returns>The promise instance.</returns>
    public ConstraintPromise<TResult> ConfigureDiagnostic(Action<Diagnostic.Builder> configure)
    {
        configure(this.Diagnostic);
        return this;
    }
}

/// <summary>
/// A <see cref="ConstraintPromise{TResult}"/> implementation that's already resolved.
/// </summary>
/// <typeparam name="TResult">The result type of the promise.</typeparam>
internal sealed class ResolvedConstraintPromise<TResult> : ConstraintPromise<TResult>
{
    public override bool IsResolved => true;
    public override TResult Result { get; }
    public override Diagnostic.Builder Diagnostic => throw new NotSupportedException();

    public ResolvedConstraintPromise(TResult result)
    {
        this.Result = result;
    }

    public override void Resolve(TResult result) => throw new NotSupportedException();
}

/// <summary>
/// A <see cref="ConstraintPromise{TResult}"/> implementation that can be resolved externally.
/// </summary>
/// <typeparam name="TResult">The result type of the promise.</typeparam>
internal sealed class ResolvableConstraintPromise<TResult> : ConstraintPromise<TResult>
{
    public override bool IsResolved => this.isResolved;
    public override TResult Result => this.isResolved
        ? this.result!
        : throw new InvalidOperationException("tried to access unresolved constraint result");
    public override Diagnostic.Builder Diagnostic { get; }

    private bool isResolved;
    private TResult? result;

    public ResolvableConstraintPromise(Diagnostic.Builder diagnosticBuilder)
    {
        this.Diagnostic = diagnosticBuilder;
    }

    public override void Resolve(TResult result)
    {
        if (this.isResolved) throw new InvalidOperationException("tried to resolve already resolved promise");
        this.result = result;
        this.isResolved = true;
    }
}

/// <summary>
/// A <see cref="ConstraintPromise{TNewResult}"/> that maps a <see cref="ConstraintPromise{TOldResult}"/>.
/// </summary>
/// <typeparam name="TOldResult">The original constraint promise result type.</typeparam>
/// <typeparam name="TOldResult">The mapped result type.</typeparam>
internal sealed class MappedConstraintPromise<TOldResult, TNewResult> : ConstraintPromise<TNewResult>
{
    public override bool IsResolved => this.underlying.IsResolved;
    public override TNewResult Result => this.mapFunc(this.underlying.Result);
    public override Diagnostic.Builder Diagnostic => this.underlying.Diagnostic;

    private readonly ConstraintPromise<TOldResult> underlying;
    private readonly Func<TOldResult, TNewResult> mapFunc;

    public MappedConstraintPromise(ConstraintPromise<TOldResult> underlying, Func<TOldResult, TNewResult> mapFunc)
    {
        this.underlying = underlying;
        this.mapFunc = mapFunc;
    }

    public override void Resolve(TNewResult result) => throw new NotSupportedException();
}
