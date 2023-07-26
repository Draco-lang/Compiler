using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// Adds the given constraint to the solver.
    /// </summary>
    /// <param name="constraint">The constraint to add.</param>
    public void Add(IConstraint constraint) =>
        this.constraints.Add(constraint);

    /// <summary>
    /// Removes the given constraint from the solver.
    /// </summary>
    /// <param name="constraint">The constraint to remove.</param>
    /// <returns>True, if <paramref name="constraint"/> could be removed, false otherwise.</returns>
    public bool Remove(IConstraint constraint) => this.constraints.Remove(constraint);

    /// <summary>
    /// Enumerates constraints in this solver.
    /// </summary>
    /// <typeparam name="TConstraint">The type of constraints to enumerate.</typeparam>
    /// <param name="filter">An optional constraint filter.</param>
    /// <returns>All constraints of type <typeparamref name="TConstraint"/> that satisfy <paramref name="filter"/>.</returns>
    public IEnumerable<TConstraint> Enumerate<TConstraint>(
        Func<TConstraint, bool>? filter = null)
    {
        filter ??= _ => true;
        return this.constraints
            .OfType<TConstraint>()
            .Where(filter);
    }

    /// <summary>
    /// Attempts to remove a constraint from this solver.
    /// </summary>
    /// <typeparam name="TConstraint">The type of constraints to remove.</typeparam>
    /// <param name="constraint">The constraint is written here, if one satisfying the conditions is found.</param>
    /// <param name="filter">An optional filter that <paramref name="constraint"/> has to satisfy.</param>
    /// <returns>True, if a constraint of type <typeparamref name="TConstraint"/> that satisfies
    /// <paramref name="filter"/> was found and removed.</returns>
    public bool TryDequeue<TConstraint>(
        [MaybeNullWhen(false)] out TConstraint constraint,
        Func<TConstraint, bool>? filter = null)
        where TConstraint : IConstraint
    {
        constraint = this
            .Enumerate(filter)
            .FirstOrDefault();
        if (constraint is not null)
        {
            this.constraints.Remove(constraint);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a same-type constraint to the solver.
    /// </summary>
    /// <param name="first">The type that is constrained to be the same as <paramref name="second"/>.</param>
    /// <param name="second">The type that is constrained to be the same as <paramref name="first"/>.</param>
    /// <returns>The promise for the constraint added.</returns>
    public IConstraintPromise<Unit> SameType(TypeSymbol first, TypeSymbol second)
    {
        var constraint = new SameTypeConstraint(ImmutableArray.Create(first, second));
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="assignedType">The type assigned.</param>
    /// <returns>The promise for the constraint added.</returns>
    public IConstraintPromise<Unit> Assignable(TypeSymbol targetType, TypeSymbol assignedType) =>
        // TODO: Hack, this is temporary until we have other constraints
        this.SameType(targetType, assignedType);

    /// <summary>
    /// Adds a common-type constraint to the solver.
    /// </summary>
    /// <param name="commonType">The common type of the provided alternative types.</param>
    /// <param name="alternativeTypes">The alternative types to find the common type of.</param>
    /// <returns>The promise of the constraint added.</returns>
    public IConstraintPromise<Unit> CommonType(TypeSymbol commonType, ImmutableArray<TypeSymbol> alternativeTypes)
    {
        // TODO: Hack, this is temporary until we have other constraints
        var constraint = new SameTypeConstraint(alternativeTypes.Prepend(commonType).ToImmutableArray());
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds a member-constraint to the solver.
    /// </summary>
    /// <param name="accessedType">The accessed object type.</param>
    /// <param name="memberName">The accessed member name.</param>
    /// <param name="memberType">The type of the member.</param>
    /// <returns>The promise of the accessed member symbol.</returns>
    public IConstraintPromise<Symbol> Member(TypeSymbol accessedType, string memberName, out TypeSymbol memberType)
    {
        memberType = this.AllocateTypeVariable();
        var constraint = new MemberConstraint(accessedType, memberName, memberType);
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds a callability constraint to the solver.
    /// </summary>
    /// <param name="calledType">The called function type.</param>
    /// <param name="args">The calling arguments.</param>
    /// <param name="returnType">The return type.</param>
    /// <returns>The promise of the constraint.</returns>
    public IConstraintPromise<Unit> Call(
        TypeSymbol calledType,
        ImmutableArray<object> args,
        out TypeSymbol returnType)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new CallConstraint(calledType, args, returnType);
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds an overload constraint to the solver.
    /// </summary>
    /// <param name="functions">The functions to choose an overload from.</param>
    /// <param name="args">The passed in arguments.</param>
    /// <param name="returnType">The return type of the call.</param>
    /// <returns>The promise for the resolved overload.</returns>
    public IConstraintPromise<FunctionSymbol> Overload(
        ImmutableArray<FunctionSymbol> functions,
        ImmutableArray<object> args,
        out TypeSymbol returnType)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new OverloadConstraint(functions, args, returnType);
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds a constraint that waits before another one finishes.
    /// </summary>
    /// <typeparam name="TAwaitedResult">The awaited constraint result.</typeparam>
    /// <typeparam name="TResult">The mapped result.</typeparam>
    /// <param name="awaited">The awaited constraint.</param>
    /// <param name="map">The function that maps the result of <paramref name="awaited"/>.</param>
    /// <returns>The promise that is resolved, when <paramref name="awaited"/>.</returns>
    public IConstraintPromise<TResult> Await<TAwaitedResult, TResult>(
        IConstraintPromise<TAwaitedResult> awaited,
        Func<TResult> map)
    {
        if (awaited.IsResolved)
        {
            // If resolved, don't bother with indirections
            var constraint = map();
            return ConstraintPromise.FromResult(constraint);
        }
        else
        {
            var constraint = new AwaitConstraint<TResult>(() => awaited.IsResolved, map);
            this.Add(constraint);
            return constraint.Promise;
        }
    }

    /// <summary>
    /// Adds a constraint, that waits until a type variable is substituted.
    /// </summary>
    /// <param name="original">The original type, usually a type variable.</param>
    /// <param name="map">Function that executes once the <paramref name="original"/> is substituted.</param>
    /// <returns>The promise of the type symbol symbol.</returns>
    public IConstraintPromise<TResult> Substituted<TResult>(TypeSymbol original, Func<TResult> map)
    {
        if (!original.IsTypeVariable)
        {
            var constraintPromise = map();
            return ConstraintPromise.FromResult(constraintPromise);
        }
        else
        {
            var constraint = new AwaitConstraint<TResult>(() => !original.Substitution.IsTypeVariable, map);
            this.Add(constraint);

            var await = new AwaitConstraint<TResult>(
                () => constraint.Promise.IsResolved && constraint.Promise.IsResolved,
                () => constraint.Promise.Result);
            this.Add(await);
            return await.Promise;
        }
    }
}
