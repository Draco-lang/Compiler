using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Factory methods for <see cref="IConstraintPromise"/>s.
/// </summary>
internal static class ConstraintPromise
{
    /// <summary>
    /// Creates an unresolved constraint promise.
    /// </summary>
    /// <typeparam name="TResult">The type the promise resolves to.</typeparam>
    /// <param name="constraint">The constraint the promise will belong to.</param>
    /// <returns>A new, unresolved <see cref="IConstraintPromise{TResult}"/>.</returns>
    public static IConstraintPromise<TResult> Create<TResult>(IConstraint constraint) =>
        new ResolvableConstraintPromise<TResult>(constraint);

    /// <summary>
    /// Constructs a constraint promise that is already resolved.
    /// </summary>
    /// <typeparam name="TResult">The result type of the promise.</typeparam>
    /// <param name="result">The resolved value.</param>
    /// <returns>The constructed promise, containing <paramref name="result"/> as the result value.</returns>
    public static IConstraintPromise<TResult> FromResult<TResult>(TResult result) =>
        new ResolvedConstraintPromise<TResult>(result);

    // TODO: Doc
    public static IConstraintPromise<TNewResult> Map<TOldResult, TNewResult>(
        this IConstraintPromise<TOldResult> promise,
        Func<TOldResult, TNewResult> map) =>
        new MappedConstraintPromise<TOldResult, TNewResult>(promise, map);

    private sealed class ResolvedConstraintPromise<TResult> : IConstraintPromise<TResult>
    {
        public bool IsResolved => true;
        public TResult Result { get; }

        public ResolvedConstraintPromise(TResult result)
        {
            this.Result = result;
        }

        public void Resolve(TResult result) =>
            throw new InvalidOperationException("can not resolve an already solved constraint");
        public void Fail(TResult result, DiagnosticBag? diagnostics) =>
            throw new InvalidOperationException("can not resolve an already solved constraint");

        public IConstraintPromise<TResult> ContinueWith(Action<TResult> continuation)
        {
            // Resolved constraint, just run
            continuation(this.Result);
            return this;
        }

        public IConstraintPromise<TResult> ConfigureDiagnostic(Action<Diagnostic.Builder> configure) => this;
        IConstraintPromise IConstraintPromise.ConfigureDiagnostic(Action<Diagnostic.Builder> configure) =>
            this.ConfigureDiagnostic(configure);
    }

    private sealed class ResolvableConstraintPromise<TResult> : IConstraintPromise<TResult>
    {
        public bool IsResolved { get; private set; }

        public TResult Result
        {
            get
            {
                if (!this.IsResolved) throw new InvalidOperationException("can not access the result of unresolved promise");
                return this.result!;
            }
            private set
            {
                if (this.IsResolved) throw new InvalidOperationException("can not set the result of an already resolved promise");
                this.result = value;
                this.IsResolved = true;
                this.continuation?.Invoke(value);
            }
        }
        private TResult? result;

        private readonly IConstraint constraint;

        private Action<TResult>? continuation;

        public ResolvableConstraintPromise(IConstraint constraint)
        {
            this.constraint = constraint;
        }

        public IConstraintPromise<TResult> ConfigureDiagnostic(Action<Diagnostic.Builder> configure)
        {
            configure(this.constraint.Diagnostic);
            return this;
        }
        IConstraintPromise IConstraintPromise.ConfigureDiagnostic(Action<Diagnostic.Builder> configure) =>
            this.ConfigureDiagnostic(configure);

        public IConstraintPromise<TResult> ContinueWith(Action<TResult> continuation)
        {
            // If resolve, invoke immediately
            if (this.IsResolved)
            {
                continuation(this.Result);
                return this;
            }

            // Otherwise, stash
            if (this.continuation is not null) throw new InvalidOperationException("promises can only have one continuation");
            this.continuation = continuation;

            return this;
        }

        public void Resolve(TResult result) => this.Result = result;
        public void Fail(TResult result, DiagnosticBag? diagnostics)
        {
            this.Result = result;
            if (diagnostics is not null)
            {
                var diag = this.constraint.Diagnostic.Build();
                diagnostics.Add(diag);
            }
        }
    }

    private sealed class MappedConstraintPromise<TOldResult, TNewResult> : IConstraintPromise<TNewResult>
    {
        public bool IsResolved => this.promise.IsResolved;
        public TNewResult Result => this.map(this.promise.Result);

        private readonly IConstraintPromise<TOldResult> promise;
        private readonly Func<TOldResult, TNewResult> map;

        public MappedConstraintPromise(IConstraintPromise<TOldResult> promise, Func<TOldResult, TNewResult> map)
        {
            this.promise = promise;
            this.map = map;
        }

        public IConstraintPromise<TNewResult> ConfigureDiagnostic(Action<Diagnostic.Builder> configure) => throw new NotImplementedException();
        public void Fail(TNewResult result, DiagnosticBag? diagnostics) => throw new NotImplementedException();
        public void Resolve(TNewResult result) => throw new NotImplementedException();
        IConstraintPromise IConstraintPromise.ConfigureDiagnostic(Action<Diagnostic.Builder> configure) => throw new NotImplementedException();
        public IConstraintPromise<TNewResult> ContinueWith(Action<TNewResult> continuation) => throw new NotImplementedException();
    }
}
