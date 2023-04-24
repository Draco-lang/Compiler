using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
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

    // Number of type variables allocated
    private int typeVariableCounter = 0;
    // Type variable substitutions
    private readonly Dictionary<TypeVariable, TypeSymbol> substitutions = new(ReferenceEqualityComparer.Instance);
    // The declared/inferred types of locals
    private readonly Dictionary<UntypedLocalSymbol, TypeSymbol> inferredLocalTypes = new(ReferenceEqualityComparer.Instance);
    // All locals that have a typed variant constructed
    private readonly Dictionary<UntypedLocalSymbol, LocalSymbol> typedLocals = new(ReferenceEqualityComparer.Instance);

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

    /// <summary>
    /// Adds a common-type constraint to the solver.
    /// </summary>
    /// <param name="commonType">The common type of the provided alternative types.</param>
    /// <param name="alternativeTypes">The alternative types to find the common type of.</param>
    /// <returns>The promise of the constraint added.</returns>
    public IConstraintPromise<Unit> CommonType(TypeSymbol commonType, ImmutableArray<TypeSymbol> alternativeTypes)
    {
        // TODO: Hack, this is temporary until we have other constraints
        var constraint = new SameTypeConstraint(this, alternativeTypes.Prepend(commonType).ToImmutableArray());
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds a member-constraint to the solver.
    /// </summary>
    /// <param name="accessedType">The accessed object type.</param>
    /// <param name="memberName">The accessed member name.</param>
    /// <returns>The promise of the accessed member symbol.</returns>
    public IConstraintPromise<ImmutableArray<Symbol>> Member(TypeSymbol accessedType, string memberName)
    {
        var constraint = new MemberConstraint(this, accessedType, memberName);
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds an overload constraint to the solver.
    /// </summary>
    /// <param name="functions">The list of functions to choose an overload from.</param>
    /// <param name="args">The passed in arguments.</param>
    /// <returns>The promise for the resolved overload.</returns>
    public IConstraintPromise<FunctionSymbol> Overload(IEnumerable<FunctionSymbol> functions, ImmutableArray<TypeSymbol> args)
    {
        var constraint = new OverloadConstraint(this, functions, args);
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
    public TypeSymbol DeclareLocal(UntypedLocalSymbol local, TypeSymbol? type)
    {
        var inferredType = type ?? this.AllocateTypeVariable();
        this.inferredLocalTypes.Add(local, inferredType);
        return inferredType;
    }

    /// <summary>
    /// Retrieves the declared/inferred type of a local.
    /// </summary>
    /// <param name="local">The local to get the type of.</param>
    /// <returns>The type of the local inferred so far.</returns>
    public TypeSymbol GetLocalType(UntypedLocalSymbol local) => this.Unwrap(this.inferredLocalTypes[local]);

    /// <summary>
    /// Retrieves the typed variant of an untyped local symbol. In case this is the first time the local is
    /// retrieved and the variable type could not be inferred, an error is reported.
    /// </summary>
    /// <param name="local">The untyped local to get the typed equivalent of.</param>
    /// <param name="diagnostics">The diagnostics to report errors to.</param>
    /// <returns>The typed equivalent of <paramref name="local"/>.</returns>
    public LocalSymbol GetTypedLocal(UntypedLocalSymbol local, DiagnosticBag diagnostics)
    {
        if (!this.typedLocals.TryGetValue(local, out var typedLocal))
        {
            var localType = this.GetLocalType(local);
            if (localType.IsTypeVariable)
            {
                // We could not infer the type
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.CouldNotInferType,
                    location: local.DeclaringSyntax.Location,
                    formatArgs: local.Name));
                // We use an error type
                localType = IntrinsicSymbols.ErrorType;
            }
            typedLocal = new SourceLocalSymbol(local, localType);
            this.typedLocals.Add(local, typedLocal);
        }
        return typedLocal;
    }

    /// <summary>
    /// Allocates a type-variable.
    /// </summary>
    /// <returns>A new, unique type-variable.</returns>
    public TypeVariable AllocateTypeVariable() => new(this.typeVariableCounter++);

    /// <summary>
    /// Unwraps the given type from potential variable substitutions.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The unwrapped type, which might be <paramref name="type"/> itself, or the substitution, if it was
    /// a type variable that already got substituted.</returns>
    public TypeSymbol Unwrap(TypeSymbol type)
    {
        // If not a type-variable, we consider it substituted
        if (type is not TypeVariable typeVar) return type;
        // If it is, but has no substitutions, just return it as-is
        if (!this.substitutions.TryGetValue(typeVar, out var substitution)) return typeVar;
        // If the substitution is also a type-variable, we prune
        if (substitution is TypeVariable)
        {
            substitution = this.Unwrap(substitution);
            this.substitutions[typeVar] = substitution;
        }
        return substitution;
    }

    /// <summary>
    /// Substitutes the given type variable for a type symbol.
    /// </summary>
    /// <param name="var">The type variable to substitute.</param>
    /// <param name="type">The substitution.</param>
    public void Substitute(TypeVariable var, TypeSymbol type) =>
        this.substitutions.Add(var, type);

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
