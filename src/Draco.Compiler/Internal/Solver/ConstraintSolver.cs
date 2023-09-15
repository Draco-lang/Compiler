using System.Collections.Generic;
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

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves sets of <see cref="IConstraint"/>s for the type-system.
/// </summary>
internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// The context being inferred.
    /// </summary>
    public SyntaxNode Context { get; }

    /// <summary>
    /// The user-friendly name of the context the solver is in.
    /// </summary>
    public string ContextName { get; }

    // The raw constraints
    private readonly HashSet<IConstraint> constraints = new(ReferenceEqualityComparer.Instance);
    // The allocated type variables
    private readonly List<TypeVariable> typeVariables = new();
    // Type variable substitutions
    private readonly Dictionary<TypeVariable, TypeSymbol> substitutions = new();
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
    /// Solves all diagnostics added to this solver.
    /// </summary>
    /// <param name="diagnostics">The bag to report diagnostics to.</param>
    public void Solve(DiagnosticBag diagnostics)
    {
        while (this.constraints.Count > 0)
        {
            // Apply rules once
            if (!this.ApplyRules(diagnostics)) break;
        }

        // Check for uninferred locals
        this.CheckForUninferredLocals(diagnostics);

        // And for failed inference
        this.CheckForIncompleteInference(diagnostics);
    }

    private void CheckForUninferredLocals(DiagnosticBag diagnostics)
    {
        foreach (var (local, localType) in this.inferredLocalTypes)
        {
            var unwrappedLocalType = localType.Substitution;
            if (unwrappedLocalType is TypeVariable typeVar)
            {
                this.UnifyAsserted(typeVar, IntrinsicSymbols.UninferredType);
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
                           || this.typeVariables.Select(t => t.Substitution).Any(t => t.IsTypeVariable);
        if (!inferenceFailed) return;

        // Couldn't solve all constraints or infer all variables
        diagnostics.Add(Diagnostic.Create(
            template: TypeCheckingErrors.InferenceIncomplete,
            location: this.Context.Location,
            formatArgs: this.ContextName));

        // To avoid major trip-ups later, we resolve all constraints to some sentinel value
        this.FailRemainingRules();
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
    public TypeSymbol GetLocalType(UntypedLocalSymbol local) => this.inferredLocalTypes[local].Substitution;

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
        if (substitution.IsTypeVariable)
        {
            substitution = substitution.Substitution;
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
    /// Unifies two types, asserting their success.
    /// </summary>
    /// <param name="first">The first type to unify.</param>
    /// <param name="second">The second type to unify.</param>
    public void UnifyAsserted(TypeSymbol first, TypeSymbol second)
    {
        if (this.Unify(first, second)) return;
        throw new System.InvalidOperationException($"could not unify {first} and {second}");
    }

    /// <summary>
    /// Attempts to unify two types.
    /// </summary>
    /// <param name="first">The first type to unify.</param>
    /// <param name="second">The second type to unify.</param>
    /// <returns>True, if unification was successful, false otherwise.</returns>
    private bool Unify(TypeSymbol first, TypeSymbol second)
    {
        first = first.Substitution;
        second = second.Substitution;

        // NOTE: Referential equality is OK here, we don't need to use SymbolEqualityComparer, this is unification
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

        case (ArrayTypeSymbol a1, ArrayTypeSymbol a2) when a1.IsGenericDefinition && a2.IsGenericDefinition:
            return a1.Rank == a2.Rank;

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
}
