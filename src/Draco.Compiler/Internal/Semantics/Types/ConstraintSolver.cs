using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;

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

// Utility functions
internal partial interface IConstraint
{
    private static Type UnwrapTypeVariable(Type type) => type is Type.Variable v
        ? v.Substitution
        : type;
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

        public void Solve() => throw new NotImplementedException();
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
}
