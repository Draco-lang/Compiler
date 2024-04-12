using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Solver.Utilities;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// Adds the given constraint to the solver.
    /// </summary>
    /// <param name="constraint">The constraint to add.</param>
    private void Add(Constraints.ConstraintBase constraint) =>
        this.store.Add(constraint);

    /// <summary>
    /// Adds a same-type constraint to the solver.
    /// </summary>
    /// <param name="first">The type that is constrained to be the same as <paramref name="second"/>.</param>
    /// <param name="second">The type that is constrained to be the same as <paramref name="first"/>.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    public void SameType(TypeSymbol first, TypeSymbol second, SyntaxNode syntax)
    {
        var constraint = new Constraints.Same(ConstraintLocator.Syntax(syntax), ImmutableArray.Create(first, second));
        this.Add(constraint);
    }

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="assignedType">The type assigned.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    public void Assignable(TypeSymbol targetType, TypeSymbol assignedType, SyntaxNode syntax) =>
        this.Assignable(targetType, assignedType, ConstraintLocator.Syntax(syntax));

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="assignedType">The type assigned.</param>
    /// <param name="locator">The locator for the constraint.</param>
    public void Assignable(TypeSymbol targetType, TypeSymbol assignedType, ConstraintLocator locator)
    {
        var constraint = new Constraints.Assignable(locator, targetType, assignedType);
        this.Add(constraint);
    }

    /// <summary>
    /// Adds a common-type constraint to the solver.
    /// </summary>
    /// <param name="commonType">The common type of the provided alternative types.</param>
    /// <param name="alternativeTypes">The alternative types to find the common type of.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    public void CommonType(
        TypeSymbol commonType,
        ImmutableArray<TypeSymbol> alternativeTypes,
        SyntaxNode syntax) => this.CommonType(commonType, alternativeTypes, ConstraintLocator.Syntax(syntax));

    /// <summary>
    /// Adds a common-type constraint to the solver.
    /// </summary>
    /// <param name="commonType">The common type of the provided alternative types.</param>
    /// <param name="alternativeTypes">The alternative types to find the common type of.</param>
    /// <param name="locator">The locator for this constraint.</param>
    public void CommonType(
        TypeSymbol commonType,
        ImmutableArray<TypeSymbol> alternativeTypes,
        ConstraintLocator locator)
    {
        var constraint = new Constraints.CommonAncestor(locator, commonType, alternativeTypes);
        this.Add(constraint);
    }

    /// <summary>
    /// Adds a member-constraint to the solver.
    /// </summary>
    /// <param name="accessedType">The accessed object type.</param>
    /// <param name="memberName">The accessed member name.</param>
    /// <param name="memberType">The type of the member.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <param name="silent">Whether to suppress diagnostics for this constraint.</param>
    /// <returns>The promise of the accessed member symbol.</returns>
    public SolverTask<Symbol> Member(
        TypeSymbol accessedType,
        string memberName,
        out TypeSymbol memberType,
        SyntaxNode syntax)
    {
        memberType = this.AllocateTypeVariable();
        var constraint = new Constraints.Member(ConstraintLocator.Syntax(syntax), accessedType, memberName, memberType);
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
    public void Call(
        TypeSymbol calledType,
        ImmutableArray<ArgumentDescription> args,
        out TypeSymbol returnType,
        SyntaxNode syntax)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new Constraints.Callable(ConstraintLocator.Syntax(syntax), calledType, args, returnType);
        this.Add(constraint);
    }

    // TODO: Do we still need the return type as an out?
    /// <summary>
    /// Adds an overload constraint to the solver.
    /// </summary>
    /// <param name="name">The function name the overload is created from.</param>
    /// <param name="functions">The functions to choose an overload from.</param>
    /// <param name="args">The passed in arguments.</param>
    /// <param name="returnType">The return type of the call.</param>
    /// <param name="syntax">The syntax that the constraint originates from.</param>
    /// <returns>The promise for the resolved overload.</returns>
    public SolverTask<FunctionSymbol> Overload(
        string name,
        ImmutableArray<FunctionSymbol> functions,
        ImmutableArray<ArgumentDescription> args,
        out TypeSymbol returnType,
        SyntaxNode syntax)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new Constraints.Overload(ConstraintLocator.Syntax(syntax), name, functions, args, returnType);
        this.Add(constraint);
        return constraint.CompletionSource.Task;
    }
}
