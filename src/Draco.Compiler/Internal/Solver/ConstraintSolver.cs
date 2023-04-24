using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves sets of <see cref="IConstraint"/>s for the type-system.
/// </summary>
internal sealed class ConstraintSolver
{
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
