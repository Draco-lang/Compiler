using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
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

    // The raw constraints and their states
    private readonly List<KeyValuePair<IConstraint, IEnumerator<SolveState>>> constraints = new();
    // The constraints that were marked for removal
    private readonly List<IConstraint> constraintsToRemove = new();
    // The constraints that were queued for insertion
    private readonly List<IConstraint> constraintsToAdd = new();
    // The allocated type variables
    private readonly List<TypeVariable> typeVariables = new();
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
    public IConstraintPromise<ImmutableArray<Symbol>> Member(TypeSymbol accessedType, string memberName, out TypeSymbol memberType)
    {
        memberType = this.AllocateTypeVariable();
        var constraint = new MemberConstraint(this, accessedType, memberName, memberType);
        this.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds a callability constraint to the solver.
    /// </summary>
    /// <param name="calledType">The called function type.</param>
    /// <param name="args">The calling argument types.</param>
    /// <param name="returnType">The return type.</param>
    /// <returns>The promise of the constraint.</returns>
    public IConstraintPromise<Unit> Call(TypeSymbol calledType, ImmutableArray<TypeSymbol> args, out TypeSymbol returnType)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new CallConstraint(this, calledType, args, returnType);
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
        ImmutableArray<TypeSymbol> args,
        out TypeSymbol returnType)
    {
        returnType = this.AllocateTypeVariable();
        var constraint = new OverloadConstraint(this, functions, args, returnType);
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
            var constraint = new AwaitConstraint<TResult>(this, () => awaited.IsResolved, map);
            this.Add(constraint);
            return constraint.Promise;
        }
    }

    /// <summary>
    /// Adds a type-constraint to the solver.
    /// </summary>
    /// <param name="original">The original type, usually a type variable.</param>
    /// <param name="map">Function that executes once the <paramref name="original"/> is substituted.</param>
    /// <returns>The promise of the type symbol symbol.</returns>
    public IConstraintPromise<TResult> Type<TResult>(TypeSymbol original, Func<IConstraintPromise<TResult>> map)
    {
        var constraint = new AwaitConstraint<IConstraintPromise<TResult>>(this, () => !this.Unwrap(original).IsTypeVariable, map);
        this.Add(constraint);

        var await = new AwaitConstraint<TResult>(this,
            () => constraint.Promise.IsResolved && constraint.Promise.Result.IsResolved,
            () => constraint.Promise.Result.Result);
        this.Add(await);
        return await.Promise;
    }

    /// <summary>
    /// Adds the given constraint to the solver.
    /// </summary>
    /// <param name="constraint">The constraint to add.</param>
    public void Add(IConstraint constraint) =>
        this.constraintsToAdd.Add(constraint);

    /// <summary>
    /// Removes the given constraint from the solver.
    /// </summary>
    /// <param name="constraint">The constraint to remove.</param>
    public void Remove(IConstraint constraint) =>
        this.constraintsToRemove.Add(constraint);

    /// <summary>
    /// Solves all diagnostics added to this solver.
    /// </summary>
    /// <param name="diagnostics">The bag to report diagnostics to.</param>
    public void Solve(DiagnosticBag diagnostics)
    {
        while (true)
        {
            // Add and removal
            this.AddAndRemoveConstraints(diagnostics);

            // Pass through all constraints
            if (!this.SolveOnce()) break;
        }

        // Check for uninferred locals
        this.CheckForUninferredLocals(diagnostics);

        // And for failed inference
        this.CheckForIncompleteInference(diagnostics);
    }

    private int GetConstraintIndex(IConstraint constraint)
    {
        for (var i = 0; i < this.constraints.Count; ++i)
        {
            if (this.constraints[i].Key == constraint) return i;
        }
        return -1;
    }

    private void AddAndRemoveConstraints(DiagnosticBag diagnostics)
    {
        foreach (var r in this.constraintsToRemove)
        {
            var idx = this.GetConstraintIndex(r);
            if (idx != -1) this.constraints.RemoveAt(idx);
        }
        foreach (var a in this.constraintsToAdd)
        {
            this.constraints.Add(new(a, a.Solve(diagnostics).GetEnumerator()));
        }
        this.constraintsToRemove.Clear();
        this.constraintsToAdd.Clear();
    }

    private bool SolveOnce()
    {
        var advanced = false;
        foreach (var (constraint, solve) in this.constraints)
        {
            while (true)
            {
                solve.MoveNext();
                var state = solve.Current;
                advanced = advanced || state != SolveState.Stale;
                if (state is SolveState.AdvancedBreak or SolveState.Stale) break;
                if (state == SolveState.Solved)
                {
                    this.Remove(constraint);
                    break;
                }
            }
        }
        return advanced;
    }

    private void CheckForUninferredLocals(DiagnosticBag diagnostics)
    {
        foreach (var (local, localType) in this.inferredLocalTypes)
        {
            var unwrappedLocalType = this.Unwrap(localType);
            if (unwrappedLocalType is TypeVariable typeVar)
            {
                this.Unify(typeVar, IntrinsicSymbols.UninferredType);
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.CouldNotInferType,
                    location: local.DeclaringSyntax.Location,
                    formatArgs: local.Name));
            }
        }
    }

    private void CheckForIncompleteInference(DiagnosticBag diagnostics)
    {
        var inferenceFailed = this.constraints.Count > 0
                           || this.typeVariables.Select(this.Unwrap).Any(t => t.IsTypeVariable);
        if (!inferenceFailed) return;

        // Couldn't solve all constraints or infer all variables
        diagnostics.Add(Diagnostic.Create(
            template: TypeCheckingErrors.InferenceIncomplete,
            location: this.Context.Location,
            formatArgs: this.ContextName));

        // To avoid major trip-ups later, we resolve all constraints to some sentinel value
        foreach (var (constraint, _) in this.constraints) constraint.FailSilently();

        // We also unify type variables with the error type
        foreach (var typeVar in this.typeVariables)
        {
            var unwrapped = this.Unwrap(typeVar);
            if (unwrapped is TypeVariable unwrappedTv) this.Unify(unwrappedTv, IntrinsicSymbols.UninferredType);
        }
    }

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
            Debug.Assert(!localType.IsTypeVariable);
            typedLocal = new SourceLocalSymbol(local, localType);
            this.typedLocals.Add(local, typedLocal);
        }
        return typedLocal;
    }

    /// <summary>
    /// Allocates a type-variable.
    /// </summary>
    /// <param name="track">True, if the type-variable should be tracked during inference.
    /// If not tracked, not substituting won't result in an error.</param>
    /// <returns>A new, unique type-variable.</returns>
    public TypeVariable AllocateTypeVariable(bool track = true)
    {
        var typeVar = new TypeVariable(this, this.typeVariables.Count);
        if (track) this.typeVariables.Add(typeVar);
        return typeVar;
    }

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
    public bool Unify(TypeSymbol first, TypeSymbol second)
    {
        first = this.Unwrap(first);
        second = this.Unwrap(second);

        // NOTE: Referential equality is OK here, we don't need to use SymbolEqualityComprer, this is unification
        if (ReferenceEquals(first, second)) return true;

        switch (first, second)
        {
        // Type variable substitution takes priority
        // so it can unify with never type and error type to stop type errors from cascading
        case (TypeVariable v1, TypeVariable v2):
        {
            // Check for circularity
            // NOTE: Referential equality is OK here, we are checking for CIRCULARITY
            // which is  referential check
            if (ReferenceEquals(v1, v2)) return true;
            this.Substitute(v1, v2);
            return true;
        }
        case (TypeVariable v, TypeSymbol other):
        {
            this.Substitute(v, other);
            return true;
        }
        case (TypeSymbol other, TypeVariable v):
        {
            this.Substitute(v, other);
            return true;
        }

        // Never type is never reached, unifies with everything
        case (NeverTypeSymbol, _):
        case (_, NeverTypeSymbol):
        // Error type unifies with everything to avoid cascading type errors
        case (ErrorTypeSymbol, _):
        case (_, ErrorTypeSymbol):
            return true;

        // NOTE: Primitives are filtered out already, along with metadata types

        case (FunctionTypeSymbol f1, FunctionTypeSymbol f2):
        {
            if (f1.Parameters.Length != f2.Parameters.Length) return false;
            for (var i = 0; i < f1.Parameters.Length; ++i)
            {
                if (!this.Unify(f1.Parameters[i].Type, f2.Parameters[i].Type)) return false;
            }
            return this.Unify(f1.ReturnType, f2.ReturnType);
        }

        case (_, _) when first.IsGenericInstance && second.IsGenericInstance:
        {
            // NOTE: Generic instances might not obey referential equality
            Debug.Assert(first.GenericDefinition is not null);
            Debug.Assert(second.GenericDefinition is not null);
            if (first.GenericArguments.Length != second.GenericArguments.Length) return false;
            if (!this.Unify(first.GenericDefinition, second.GenericDefinition)) return false;
            for (var i = 0; i < first.GenericArguments.Length; ++i)
            {
                if (!this.Unify(first.GenericArguments[i], second.GenericArguments[i])) return false;
            }
            return true;
        }

        default:
            return false;
        }
    }

    /// <summary>
    /// Scores a function call argument.
    /// </summary>
    /// <param name="param">The function parameter.</param>
    /// <param name="argType">The passed in argument type.</param>
    /// <returns>The score of the match.</returns>
    public int? ScoreArgument(ParameterSymbol param, TypeSymbol argType)
    {
        var paramType = this.Unwrap(param.Type);
        argType = this.Unwrap(argType);

        // If the parameter or argument is still a type parameter, we can't score it
        if (paramType.IsTypeVariable || argType.IsTypeVariable) return null;

        // Exact equality is max score
        if (SymbolEqualityComparer.Default.Equals(paramType, argType)) return 16;

        // Type parameter match is half score
        if (paramType is TypeParameterSymbol) return 8;

        // Otherwise, no match
        return 0;
    }
}
