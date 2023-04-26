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
            }
        }
        private TResult? result;

        private readonly IConstraint constraint;

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
}
