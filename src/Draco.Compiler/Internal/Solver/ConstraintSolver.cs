using System.Collections.Generic;
using System.Linq;
using Draco.Chr.Constraints;
using Draco.Chr.Solve;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;
using IChrSolver = Draco.Chr.Solve.ISolver;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves sets of <see cref="IConstraint"/>s for the type-system.
/// </summary>
internal sealed partial class ConstraintSolver(SyntaxNode context, string contextName)
{
    /// <summary>
    /// The context being inferred.
    /// </summary>
    public SyntaxNode Context { get; } = context;

    /// <summary>
    /// The user-friendly name of the context the solver is in.
    /// </summary>
    public string ContextName { get; } = contextName;

    // The constraint store
    private readonly ConstraintStore constraintStore = new();
    // The allocated type variables
    private readonly List<TypeVariable> typeVariables = [];
    // The registered local variables
    private readonly List<LocalSymbol> localVariables = [];

    /// <summary>
    /// Constructs an argument for a call constraint.
    /// </summary>
    /// <param name="syntax">The argument syntax.</param>
    /// <param name="type">The argument type.</param>
    public Argument Arg(SyntaxNode? syntax, TypeSymbol type) => new(syntax, type);

    /// <summary>
    /// Constructs an argument for a call constraint.
    /// </summary>
    /// <param name="syntax">The argument syntax.</param>
    /// <param name="expression">The argument expression.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The constructed argument descriptor.</returns>
    public Argument Arg(SyntaxNode? syntax, BindingTask<BoundExpression> expression, DiagnosticBag diagnostics) =>
        new(syntax, expression.GetResultType(syntax, this, diagnostics));

    /// <summary>
    /// Constructs an argument for a call constraint.
    /// </summary>
    /// <param name="syntax">The argument syntax.</param>
    /// <param name="lvalue">The argument lvalue.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The constructed argument descriptor.</returns>
    public Argument Arg(SyntaxNode? syntax, BindingTask<BoundLvalue> lvalue, DiagnosticBag diagnostics) =>
        new(syntax, lvalue.GetResultType(syntax, this, diagnostics));

    /// <summary>
    /// Solves all diagnostics added to this solver.
    /// </summary>
    /// <param name="diagnostics">The bag to report diagnostics to.</param>
    public void Solve(DiagnosticBag diagnostics)
    {
        var solver = new DefinitionOrderSolver(this.ConstructRules(diagnostics));
        solver.Solve(this.constraintStore);

        // Check for uninferred locals
        this.CheckForUninferredLocals(diagnostics);

        // And for failed inference
        this.CheckForIncompleteInference(diagnostics, solver);
    }

    private void CheckForUninferredLocals(DiagnosticBag diagnostics)
    {
        foreach (var local in this.localVariables)
        {
            var unwrappedLocalType = local.Type.Substitution;
            if (unwrappedLocalType is TypeVariable typeVar)
            {
                UnifyAsserted(typeVar, WellKnownTypes.UninferredType);
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.CouldNotInferType,
                    location: local.DeclaringSyntax?.Location,
                    formatArgs: local.Name));
            }
        }
    }

    private void CheckForIncompleteInference(DiagnosticBag diagnostics, IChrSolver solver)
    {
        var inferenceFailed = this.constraintStore.Count > 0
                           || this.typeVariables.Select(t => t.Substitution).Any(t => t.IsTypeVariable);
        if (!inferenceFailed) return;

        // Couldn't solve all constraints or infer all variables
        diagnostics.Add(Diagnostic.Create(
            template: TypeCheckingErrors.InferenceIncomplete,
            location: this.Context.Location,
            formatArgs: this.ContextName));

        this.FailRemainingRules(solver);
    }

    private void FailRemainingRules(IChrSolver solver)
    {
        // We unify type variables with the error type
        foreach (var typeVar in this.typeVariables)
        {
            var unwrapped = typeVar.Substitution;
            if (unwrapped is TypeVariable unwrappedTv) UnifyAsserted(unwrappedTv, WellKnownTypes.UninferredType);
        }

        // Assume this solves everything
        solver.Solve(this.constraintStore);

        if (this.constraintStore.Count > 0)
        {
            throw new System.InvalidOperationException("fallback operation could not solve all constraints");
        }
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
        if (track) this.Track(typeVar);
        return typeVar;
    }

    /// <summary>
    /// Adds a type-variable to the solver for tracking.
    /// </summary>
    /// <param name="typeVariable">The type-variable to track.</param>
    public void Track(TypeVariable typeVariable) => this.typeVariables.Add(typeVariable);
}
