using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Semantics.Types;

// TODO: This interface is way too simplistic for now
/// <summary>
/// Represents a single type constraint.
/// </summary>
internal partial interface IConstraint
{
    /// <summary>
    /// The diagnostic where the constraint can signal an error.
    /// </summary>
    public Diagnostic.Builder Diagnostic { get; }

    /// <summary>
    /// Solves this type constraint.
    /// </summary>
    public void Solve();
}

internal partial interface IConstraint
{
    /// <summary>
    /// A constraint that guarantees that two types are the same.
    /// </summary>
    public sealed class SameType : IConstraint
    {
        private enum UnificationError
        {
            TypeMismatch,
            ParameterCountMismatch,
        }

        public Type First { get; }
        public Type Second { get; }
        public Diagnostic.Builder Diagnostic { get; } = new();

        public SameType(Type first, Type second)
        {
            this.First = first;
            this.Second = second;
        }

        public void Solve()
        {
            var error = Unify(this.First, this.Second);
            if (error is not null)
            {
                this.Diagnostic.WithMessage(
                    template: TypeCheckingErrors.TypeMismatch,
                    args: new[] { this.First, this.Second });
            }
        }

        private static UnificationError? Unify(Type left, Type right)
        {
            static UnificationError? Ok() => null;
            static UnificationError? Error(UnificationError err) => err;

            left = left.UnwrapTypeVariable;
            right = right.UnwrapTypeVariable;

            switch (left, right)
            {
            case (Type.Variable v1, Type.Variable v2):
            {
                // Don't create a cycle
                if (!ReferenceEquals(v1, v2)) v1.Substitution = v2;
                return Ok();
            }

            // Variable substitution
            case (Type.Variable v1, _):
            {
                v1.Substitution = right;
                return Ok();
            }
            case (_, Type.Variable v2):
            {
                v2.Substitution = left;
                return Ok();
            }

            // Swallow cascading errors
            case (Type.Error, _):
            case (_, Type.Error):
            {
                return Ok();
            }

            // Never-type is compativle with everything
            case (Type.Never, _):
            case (_, Type.Never):
            {
                return Ok();
            }

            case (Type.Builtin b1, Type.Builtin b2):
            {
                if (b1.Type != b2.Type) return Error(UnificationError.TypeMismatch);
                return Ok();
            }

            case (Type.Function f1, Type.Function f2):
            {
                if (f1.Params.Length != f2.Params.Length) return Error(UnificationError.ParameterCountMismatch);
                var returnError = Unify(f1.Return, f2.Return);
                if (returnError is not null) return returnError;
                for (var i = 0; i < f1.Params.Length; ++i)
                {
                    var parameterError = Unify(f1.Params[i], f2.Params[i]);
                    if (parameterError is not null) return parameterError;
                }
                return Ok();
            }

            default:
            {
                return Error(UnificationError.TypeMismatch);
            }
            }
        }
    }
}

/// <summary>
/// Interface for solver promises.
/// </summary>
internal interface IConstraintSolverPromise
{
    /// <summary>
    /// The constraint being solved.
    /// </summary>
    public IConstraint Constraint { get; }

    /// <summary>
    /// The builder for a <see cref="Diagnostics.Diagnostic"/>.
    /// </summary>
    public Diagnostic.Builder Diagnostic { get; }
}

/// <summary>
/// Represents a promise to a <see cref="IConstraint"/> being solved.
/// </summary>
/// <typeparam name="TResult">The result type of the promise.</typeparam>
internal sealed class ConstraintSolverPromise<TResult> : IConstraintSolverPromise
{
    /// <summary>
    /// The result of the promise.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// The constraint being solved.
    /// </summary>
    public IConstraint Constraint { get; }

    /// <summary>
    /// The builder for a <see cref="Diagnostics.Diagnostic"/>.
    /// </summary>
    public Diagnostic.Builder Diagnostic { get; private set; }

    public ConstraintSolverPromise(TResult result, IConstraint constraint)
    {
        this.Result = result;
        this.Constraint = constraint;
        this.Diagnostic = constraint.Diagnostic;
    }

    public ConstraintSolverPromise<TResult> ConfigureDiagnostic(Func<Diagnostic.Builder, Diagnostic.Builder> configure)
    {
        this.Diagnostic = configure(this.Diagnostic);
        return this;
    }
}

/// <summary>
/// Solves type-constraints.
/// </summary>
internal sealed class ConstraintSolver
{
    private ImmutableArray<Diagnostic>? diagnostics;
    public ImmutableArray<Diagnostic> Diagnostics => this.diagnostics ??= this.Solve();

    private readonly List<IConstraintSolverPromise> promises = new();

    public ImmutableArray<Diagnostic> Solve()
    {
        var diags = ImmutableArray.CreateBuilder<Diagnostic>();
        // TODO: This is way too simplistic, only temporary
        foreach (var promise in this.promises)
        {
            promise.Constraint.Solve();
            if (promise.Diagnostic.TryBuild(out var diag)) diags.Add(diag);
        }
        return diags.ToImmutable();
    }

    public ConstraintSolverPromise<Type> Same(Type first, Type second)
    {
        var constraint = new IConstraint.SameType(first, second);
        var promise = new ConstraintSolverPromise<Type>(first, constraint);
        this.promises.Add(promise);
        return promise;
    }

    public ConstraintSolverPromise<Type> Call(Type calledType, ImmutableArray<Type> argTypes)
    {
        // TODO: Hack, this is temporary until we have other constraints
        var returnType = new Type.Variable(null);
        var callSite = new Type.Function(argTypes, returnType);
        var promise = this.Same(callSite, calledType);
        return new(returnType, promise.Constraint);
    }
}
