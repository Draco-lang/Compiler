using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves sets of <see cref="IConstraint"/>s for the type-system.
/// </summary>
internal sealed class ConstraintSolver
{
    /// <summary>
    /// The context being inferred.
    /// </summary>
    public SyntaxNode Context { get; }

    /// <summary>
    /// The user-friendly name of the context the solver is in.
    /// </summary>
    public string ContextName { get; }

    public ConstraintSolver(SyntaxNode context, string contextName)
    {
        this.Context = context;
        this.ContextName = contextName;
    }

    /// <summary>
    /// Adds a same-type constraint to the solver.
    /// </summary>
    /// <param name="first">The type that is constrained to be the same as <paramref name="second"/>.</param>
    /// <param name="second">The type that is constrained to be the same as <paramref name="first"/>.</param>
    /// <returns>The promise for the constraint added.</returns>
    public IConstraintPromise<Unit> SameType(TypeSymbol first, TypeSymbol second)
    {
        var constraint = new SameTypeConstraint(this, ImmutableArray.Create(first, second));
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

    public IConstraintPromise<Unit> CommonType(TypeSymbol commonType, ImmutableArray<TypeSymbol> alternativeTypes)
    {
        // TODO: Hack, this is temporary until we have other constraints
        var constraint = new SameTypeConstraint(this, alternativeTypes.Prepend(commonType).ToImmutableArray());
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds the given constraint to the solver.
    /// </summary>
    /// <param name="constraint">The constraint to add.</param>
    public void Add(IConstraint constraint) =>
        throw new NotImplementedException();

    /// <summary>
    /// Removes the given constraint from the solver.
    /// </summary>
    /// <param name="constraint">The constraint to remove.</param>
    public void Remove(IConstraint constraint) =>
        throw new NotImplementedException();

    /// <summary>
    /// Solves all diagnostics added to this solver.
    /// </summary>
    /// <param name="diagnostics">The bag to report diagnostics to.</param>
    public void Solve(DiagnosticBag diagnostics) =>
        throw new NotImplementedException();

    /// <summary>
    /// Adds a local to the solver.
    /// </summary>
    /// <param name="local">The symbol of the untyped local.</param>
    /// <param name="type">The optional declared type for the local.</param>
    /// <returns>The type the local was declared with.</returns>
    public TypeSymbol DeclareLocal(UntypedLocalSymbol local, TypeSymbol? type) =>
        throw new NotImplementedException();

    /// <summary>
    /// Retrieves the declared/inferred type of a local.
    /// </summary>
    /// <param name="local">The local to get the type of.</param>
    /// <returns>The type of the local inferred so far.</returns>
    public TypeSymbol GetLocalType(UntypedLocalSymbol local) =>
        throw new NotImplementedException();

    /// <summary>
    /// Retrieves the typed variant of an untyped local symbol. In case this is the first time the local is
    /// retrieved and the variable type could not be inferred, an error is reported.
    /// </summary>
    /// <param name="local">The untyped local to get the typed equivalent of.</param>
    /// <param name="diagnostics">The diagnostics to report errors to.</param>
    /// <returns>The typed equivalent of <paramref name="local"/>.</returns>
    public LocalSymbol GetTypedLocal(UntypedLocalSymbol local, DiagnosticBag diagnostics) =>
        throw new NotImplementedException();

    /// <summary>
    /// Allocates a type-variable.
    /// </summary>
    /// <returns>A new, unique type-variable.</returns>
    public TypeVariable AllocateTypeVariable() =>
        throw new NotImplementedException();

    /// <summary>
    /// Unwraps the given type from potential variable substitutions.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The unwrapped type, which might be <paramref name="type"/> itself, or the substitution, if it was
    /// a type variable that already got substituted.</returns>
    public TypeSymbol Unwrap(TypeSymbol type) =>
        throw new NotImplementedException();

    /// <summary>
    /// Substitutes the given type variable for a type symbol.
    /// </summary>
    /// <param name="var">The type variable to substitute.</param>
    /// <param name="type">The substitution.</param>
    public void Substitute(TypeVariable var, TypeSymbol type) =>
        throw new NotImplementedException();

    /// <summary>
    /// Attempts to unify two types.
    /// </summary>
    /// <param name="first">The first type to unify.</param>
    /// <param name="second">The second type to unify.</param>
    /// <returns>True, if unification was successful, false otherwise.</returns>
    public bool Unify(TypeSymbol first, TypeSymbol second) =>
        throw new NotImplementedException();

    /// <summary>
    /// Prints the constraint graph as a DOT graph.
    /// </summary>
    /// <returns>The DOT graph of the constraints within this solver.</returns>
    public string ConstraintGraphToDot() =>
        throw new NotImplementedException();
}
