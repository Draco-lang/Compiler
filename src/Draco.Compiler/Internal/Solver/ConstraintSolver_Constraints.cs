using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
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
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <returns>The promise for the constraint added.</returns>
    public BindingTask<Unit> SameType(TypeSymbol first, TypeSymbol second, SyntaxNode syntax)
    {
        var constraint = new SameTypeConstraint(this, ImmutableArray.Create(first, second), ConstraintLocator.Syntax(syntax));
        this.Add(constraint);
        return constraint.CompletionSource.Task;
    }

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="assignedType">The type assigned.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <returns>The promise for the constraint added.</returns>
    public BindingTask<Unit> Assignable(TypeSymbol targetType, TypeSymbol assignedType, SyntaxNode syntax) =>
        this.Assignable(targetType, assignedType, ConstraintLocator.Syntax(syntax));

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="assignedType">The type assigned.</param>
    /// <param name="locator">The locator for the constraint.</param>
    /// <returns>The promise for the constraint added.</returns>
    public BindingTask<Unit> Assignable(TypeSymbol targetType, TypeSymbol assignedType, ConstraintLocator locator)
    {
        var constraint = new AssignableConstraint(this, targetType, assignedType, locator);
        this.Add(constraint);
        return constraint.CompletionSource.Task;
    }

    /// <summary>
    /// Adds a common-type constraint to the solver.
    /// </summary>
    /// <param name="commonType">The common type of the provided alternative types.</param>
    /// <param name="alternativeTypes">The alternative types to find the common type of.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <returns>The promise of the constraint added.</returns>
    public BindingTask<Unit> CommonType(
        TypeSymbol commonType,
        ImmutableArray<TypeSymbol> alternativeTypes,
        SyntaxNode syntax) => this.CommonType(commonType, alternativeTypes, ConstraintLocator.Syntax(syntax));

    /// <summary>
    /// Adds a common-type constraint to the solver.
    /// </summary>
    /// <param name="commonType">The common type of the provided alternative types.</param>
    /// <param name="alternativeTypes">The alternative types to find the common type of.</param>
    /// <param name="locator">The locator for this constraint.</param>
    /// <returns>The promise of the constraint added.</returns>
    public BindingTask<Unit> CommonType(
        TypeSymbol commonType,
        ImmutableArray<TypeSymbol> alternativeTypes,
        ConstraintLocator locator)
    {
        var constraint = new CommonTypeConstraint(this, commonType, alternativeTypes, locator);
        this.Add(constraint);
        return constraint.CompletionSource.Task;
    }

    /// <summary>
    /// Adds a member-constraint to the solver.
    /// </summary>
    /// <param name="accessedType">The accessed object type.</param>
    /// <param name="memberName">The accessed member name.</param>
    /// <param name="memberType">The type of the member.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <returns>The promise of the accessed member symbol.</returns>
    public BindingTask<Symbol> Member(
        TypeSymbol accessedType,
        string memberName,
        out TypeSymbol memberType,
        SyntaxNode syntax)
    {
        memberType = this.AllocateTypeVariable();
        var constraint = new MemberConstraint(this, accessedType, memberName, memberType, ConstraintLocator.Syntax(syntax));
        this.Add(constraint);
        return constraint.CompletionSource.Task;
    }

    /// <summary>
    /// Adds a callability constraint to the solver.
    /// </summary>
    /// <param name="calledType">The called function type.</param>
    /// <param name="args">The calling arguments.</param>
    /// <param name="returnType">The return type.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <returns>The promise of the constraint.</returns>
    public BindingTask<Unit> Call(
        TypeSymbol calledType,
        ImmutableArray<object> args,
        out TypeSymbol returnType,
        SyntaxNode syntax)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new CallConstraint(this, calledType, args, returnType, ConstraintLocator.Syntax(syntax));
        this.Add(constraint);
        return constraint.CompletionSource.Task;
    }

    /// <summary>
    /// Adds an overload constraint to the solver.
    /// </summary>
    /// <param name="name">The function name the overload is created from.</param>
    /// <param name="functions">The functions to choose an overload from.</param>
    /// <param name="args">The passed in arguments.</param>
    /// <param name="returnType">The return type of the call.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <returns>The promise for the resolved overload.</returns>
    public BindingTask<FunctionSymbol> Overload(
        string name,
        ImmutableArray<FunctionSymbol> functions,
        ImmutableArray<object> args,
        out TypeSymbol returnType,
        SyntaxNode syntax)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new OverloadConstraint(this, name, functions, args, returnType, ConstraintLocator.Syntax(syntax));
        this.Add(constraint);
        return constraint.CompletionSource.Task;
    }
}
