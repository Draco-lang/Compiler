using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves sets of <see cref="IConstraint"/>s for the type-system.
/// </summary>
internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// Represents an argument for a call.
    /// </summary>
    /// <param name="Syntax">The syntax of the argument, if any.</param>
    /// <param name="Type">The type of the argument.</param>
    public readonly record struct Argument(SyntaxNode? Syntax, TypeSymbol Type);

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
    // The registered local variables
    private readonly List<LocalSymbol> localVariables = new();

    public ConstraintSolver(SyntaxNode context, string contextName)
    {
        this.Context = context;
        this.ContextName = contextName;
    }

    // TODO: Docs
    public Argument Arg(SyntaxNode? syntax, BindingTask<BoundExpression> expression, DiagnosticBag diagnostics) =>
        new(syntax, expression.GetResultType(syntax, this, diagnostics));

    // TODO: Docs
    public Argument Arg(SyntaxNode? syntax, BindingTask<BoundLvalue> lvalue, DiagnosticBag diagnostics) =>
        new(syntax, lvalue.GetResultType(syntax, this, diagnostics));

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
        foreach (var local in this.localVariables)
        {
            var unwrappedLocalType = local.Type.Substitution;
            if (unwrappedLocalType is TypeVariable typeVar)
            {
                UnifyAsserted(typeVar, IntrinsicSymbols.UninferredType);
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.CouldNotInferType,
                    location: local.DeclaringSyntax?.Location,
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
    public void DeclareLocal(LocalSymbol local) => this.localVariables.Add(local);

    /// <summary>
    /// Allocates a type-variable.
    /// </summary>
    /// <param name="track">True, if the type-variable should be tracked during inference.
    /// If not tracked, not substituting won't result in an error.</param>
    /// <returns>A new, unique type-variable.</returns>
    public TypeVariable AllocateTypeVariable(bool track = true)
    {
        var typeVar = new TypeVariable(this.typeVariables.Count);
        if (track) this.typeVariables.Add(typeVar);
        return typeVar;
    }

    /// <summary>
    /// Unwraps the potential type-variable until it is a non-type-variable type.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The task that completes when <paramref name="type"/> is subsituted as a non-type-variable.</returns>
    public static async SolverTask<TypeSymbol> Substituted(TypeSymbol type)
    {
        while (type is TypeVariable tv) type = await tv.Substituted;
        return type;
    }

    /// <summary>
    /// Unifies two types, asserting their success.
    /// </summary>
    /// <param name="first">The first type to unify.</param>
    /// <param name="second">The second type to unify.</param>
    public static void UnifyAsserted(TypeSymbol first, TypeSymbol second)
    {
        if (Unify(first, second)) return;
        throw new System.InvalidOperationException($"could not unify {first} and {second}");
    }

    /// <summary>
    /// Attempts to unify two types.
    /// </summary>
    /// <param name="first">The first type to unify.</param>
    /// <param name="second">The second type to unify.</param>
    /// <returns>True, if unification was successful, false otherwise.</returns>
    public static bool Unify(TypeSymbol first, TypeSymbol second)
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
            v1.Substitute(v2);
            return true;
        }
        case (TypeVariable v, TypeSymbol other):
        {
            v.Substitute(other);
            return true;
        }
        case (TypeSymbol other, TypeVariable v):
        {
            v.Substitute(other);
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
                if (!Unify(f1.Parameters[i].Type, f2.Parameters[i].Type)) return false;
            }
            return Unify(f1.ReturnType, f2.ReturnType);
        }

        case (_, _) when first.IsGenericInstance && second.IsGenericInstance:
        {
            // NOTE: Generic instances might not obey referential equality
            Debug.Assert(first.GenericDefinition is not null);
            Debug.Assert(second.GenericDefinition is not null);
            if (first.GenericArguments.Length != second.GenericArguments.Length) return false;
            if (!Unify(first.GenericDefinition, second.GenericDefinition)) return false;
            for (var i = 0; i < first.GenericArguments.Length; ++i)
            {
                if (!Unify(first.GenericArguments[i], second.GenericArguments[i])) return false;
            }
            return true;
        }

        default:
            return false;
        }
    }
}
