using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Solves type-constraint problems for the binder.
/// </summary>
internal sealed partial class ConstraintSolver
{
    private enum SolveState
    {
        Stale,
        Progressing,
        Finished,
    }

    /// <summary>
    /// Allocates a type-variable.
    /// </summary>
    public TypeVariable NextTypeVariable => new(this.typeVariableCounter++);
    private int typeVariableCounter = 0;

    /// <summary>
    /// The user-friendly name of the context the solver is in.
    /// </summary>
    public string ContextName { get; }

    // The list of raw constraints
    private readonly List<Constraint> constraints = new();
    // Type variable substitutions
    private readonly Dictionary<TypeVariable, Type> substitutions = new(ReferenceEqualityComparer.Instance);
    // The declared/inferred types of locals
    private readonly Dictionary<UntypedLocalSymbol, Type> inferredLocalTypes = new(ReferenceEqualityComparer.Instance);
    // All locals that have a typed variant constructed
    private readonly Dictionary<UntypedLocalSymbol, LocalSymbol> typedLocals = new(ReferenceEqualityComparer.Instance);

    public ConstraintSolver(string contextName)
    {
        this.ContextName = contextName;
    }

    /// <summary>
    /// Solves all constraints within the solver.
    /// </summary>
    public void Solve(DiagnosticBag diagnostics)
    {
        while (true)
        {
            var advanced = false;
            for (var i = 0; i < this.constraints.Count;)
            {
                var state = this.Solve(diagnostics, this.constraints[i]);
                advanced = advanced || state != SolveState.Stale;

                if (state == SolveState.Finished) this.constraints.RemoveAt(i);
                else ++i;
            }
            if (!advanced) break;
        }
        if (this.constraints.Count > 0)
        {
            // Couldn't solve all constraints
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.InferenceIncomplete,
                location: null,
                formatArgs: this.ContextName));

            // To avoid major trip-ups later, we resolve all constraints to some sentinel value
            foreach (var constraint in this.constraints) this.FailSilently(constraint);
        }
    }

    /// <summary>
    /// Adds a local to the solver.
    /// </summary>
    /// <param name="local">The symbol of the untyped local.</param>
    /// <param name="type">The optional declared type for the local.</param>
    /// <returns>The type the local was declared with.</returns>
    public Type AddLocal(UntypedLocalSymbol local, Type? type)
    {
        var inferredType = type ?? this.NextTypeVariable;
        this.inferredLocalTypes.Add(local, inferredType);
        return inferredType;
    }

    /// <summary>
    /// Retrieves the declared/inferred type of a local.
    /// </summary>
    /// <param name="local">The local to get the type of.</param>
    /// <returns>The type of the local inferred so far.</returns>
    public Type GetLocal(UntypedLocalSymbol local) => this.Unwrap(this.inferredLocalTypes[local]);

    /// <summary>
    /// Retrieves the typed variant of an untyped local. In case this is the first time the local is
    /// retrieved and the variable type could not be inferred, an error is reported.
    /// </summary>
    /// <param name="local">The untyped local to get the typed equivalent of.</param>
    /// <param name="diagnostics">The diagnostics to report errors to.</param>
    /// <returns>The typed equivalent of <paramref name="local"/>.</returns>
    public LocalSymbol GetTypedLocal(UntypedLocalSymbol local, DiagnosticBag diagnostics)
    {
        if (!this.typedLocals.TryGetValue(local, out var typedLocal))
        {
            var localType = this.GetLocal(local);
            if (localType.IsTypeVariable)
            {
                // We could not infer the type
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.CouldNotInferType,
                    location: local.DeclarationSyntax.Location,
                    formatArgs: local.Name));
                // We use an error type
                localType = IntrinsicTypes.Error;
            }
            typedLocal = new SourceLocalSymbol(local, localType);
            this.typedLocals.Add(local, typedLocal);
        }
        return typedLocal;
    }

    /// <summary>
    /// Unwraps a potential type-variable into the inferred type.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <returns>The inferred type.</returns>
    public Type Unwrap(Type type)
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
    /// Substitutes the given type-variable for another type.
    /// </summary>
    /// <param name="typeVar">The type-variable to substitute.</param>
    /// <param name="type">The type to subsitute <paramref name="typeVar"/> for.</param>
    private void Substitute(TypeVariable typeVar, Type type) =>
        this.substitutions.Add(typeVar, type);

    /// <summary>
    /// Adds a same-type constraint to the solver.
    /// </summary>
    /// <param name="first">The type that is constrained to be the same as <paramref name="second"/>.</param>
    /// <param name="second">The type that is constrained to be the same as <paramref name="first"/>.</param>
    /// <returns>The promise for the constraint added.</returns>
    public ConstraintPromise<Type> SameType(Type first, Type second)
    {
        var constraint = new SameTypeConstraint(first, second);
        this.constraints.Add(constraint);
        return constraint.Promise;
    }

    /// <summary>
    /// Adds an assignable constraint to the solver.
    /// </summary>
    /// <param name="targetType">The type being assigned to.</param>
    /// <param name="assignedType">The type assigned.</param>
    /// <returns>The promise for the constraint added.</returns>
    public ConstraintPromise<Type> Assignable(Type targetType, Type assignedType) =>
        // TODO: Hack, this is temporary until we have other constraints
        this.SameType(targetType, assignedType);

    /// <summary>
    /// Adds a common-type constraint to the solver.
    /// </summary>
    /// <param name="first">The first type to search a common type for.</param>
    /// <param name="second">The second type to search a common type for.</param>
    /// <returns>The promise for the constraint added.</returns>
    public ConstraintPromise<Type> CommonType(Type first, Type second) =>
        // TODO: Hack, this is temporary until we have other constraints
        this.SameType(first, second);

    /// <summary>
    /// Adds a call constraint to the solver.
    /// </summary>
    /// <param name="functionType">The function type being called.</param>
    /// <param name="argTypes">The argument types that the function is called with.</param>
    /// <returns>The promise for the constraint added, containing the return type.</returns>
    public ConstraintPromise<Type> Call(Type functionType, IEnumerable<Type> argTypes)
    {
        // We can save on type variables here
        var returnType = functionType is FunctionType f ? f.ReturnType : this.NextTypeVariable;
        // TODO: Hack, this is temporary until we have other constraints
        // Construct a type for the call-site
        var callSiteType = new FunctionType(argTypes.ToImmutableArray(), returnType);
        var constraint = new SameTypeConstraint(functionType, callSiteType);
        this.constraints.Add(constraint);
        // We map the call-site to the return-type
        return constraint.Promise.Map(_ => returnType);
    }

    /// <summary>
    /// Adds an overload constraint to the solver.
    /// </summary>
    /// <param name="functions">The list of functions to choose an overload from.</param>
    /// <returns>The promise for the constraint added along with the call-site type.</returns>
    public (ConstraintPromise<FunctionSymbol> Symbol, Type CallSite) Overload(IEnumerable<FunctionSymbol> functions)
    {
        var callSite = this.NextTypeVariable;
        var constraint = new OverloadConstraint(functions, callSite);
        this.constraints.Add(constraint);
        return (constraint.Promise, constraint.CallSite);
    }

    /// <summary>
    /// Utility to construct an overload constraint from a symbol.
    /// </summary>
    /// <param name="symbol">The symbol that is either a function declaration or an overload.</param>
    /// <returns>The promise for the constraint added along with the call-site type.</returns>
    public (ConstraintPromise<FunctionSymbol> Symbol, Type CallSite) Overload(Symbol symbol) => symbol switch
    {
        FunctionSymbol function => (ConstraintPromise.FromResult(function), function.Type),
        OverloadSymbol overload => this.Overload(overload.Functions),
        _ => throw new System.ArgumentOutOfRangeException(nameof(symbol)),
    };
}
