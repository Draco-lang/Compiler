using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.Constraints;
using Draco.Compiler.Internal.Solver.OverloadResolution;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    /// <summary>
    /// Tries to apply a rule to the current set of constraints.
    /// This is a fixpoint iteration method. Once it returns false, no more rules can be applied.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>True, if a change was made, false otherwise.</returns>
    private bool ApplyRulesOnce(DiagnosticBag diagnostics)
    {
        if (Same()) return true;
        if (Assignable()) return true;
        if (CommonAncestor()) return true;
        if (Member()) return true;
        if (Indexer()) return true;
        if (Callable()) return true;
        if (OverloadRefine()) return true;
        if (Overload()) return true;
        if (MergeAssignables()) return true;
        if (SingleAssignable()) return true;
        if (CommonAncestorSingleNonTypeVar()) return true;
        if (CommonAncestorIsGround()) return true;
        return false;

        // Trivial same-type constraint, unify all
        bool Same()
        {
            if (!this.constraintStore.TryRemove<Same>(out var same)) return false;
            for (var i = 1; i < same.Types.Length; ++i)
            {
                if (Unify(same.Types[0], same.Types[i])) continue;
                // Type-mismatch
                same.ReportDiagnostic(diagnostics, builder => builder
                    .WithFormatArgs(same.Types[0].Substitution, same.Types[i].Substitution));
                break;
            }
            return true;
        }

        // Assignable can be resolved directly, if both types are ground-types
        bool Assignable()
        {
            if (!this.constraintStore.TryRemove<Assignable>(out var assignable, assignable => assignable.TargetType.IsGroundType
                                                                                           && assignable.AssignedType.IsGroundType))
            {
                return false;
            }
            var targetType = assignable.TargetType;
            var assignedType = assignable.AssignedType;
            if (!SymbolEqualityComparer.Default.IsBaseOf(targetType, assignedType))
            {
                // Error
                assignable.ReportDiagnostic(diagnostics, diag => diag
                    .WithFormatArgs(targetType, assignedType));
            }
            return true;
        }

        // If all types are ground-types, common-type constraints are trivial
        bool CommonAncestor()
        {
            if (!this.constraintStore.TryRemove<CommonAncestor>(out var common, common => common.AlternativeTypes.All(t => t.IsGroundType))) return false;
            foreach (var type in common.AlternativeTypes)
            {
                if (!common.AlternativeTypes.All(t => SymbolEqualityComparer.Default.IsBaseOf(type, t))) continue;
                // Found a good common type
                this.Assignable(common.CommonType, type, ConstraintLocator.Constraint(common));
                return true;
            }
            // No common type found
            common.ReportDiagnostic(diagnostics, builder => builder
                .WithFormatArgs(string.Join(", ", common.AlternativeTypes)));
            // Stop cascading uninferred type
            UnifyWithError(common.CommonType);
            return true;
        }

        // Member constraints are trivial, if the receiver is not a type-variable
        bool Member()
        {
            if (!this.constraintStore.TryRemove<Member>(out var member, member => !member.Receiver.Substitution.IsTypeVariable)) return false;
            var accessed = member.Receiver.Substitution;
            // Don't propagate type errors
            if (accessed.IsError)
            {
                UnifyWithError(member.MemberType);
                member.CompletionSource.SetResult(ErrorMemberSymbol.Instance);
                return true;
            }

            // Not a type variable, we can look into members
            var membersWithName = accessed.AllMembers
                .Where(m => m.Name == member.MemberName)
                .ToImmutableArray();
            if (membersWithName.Length == 0)
            {
                // We have special member constraints that are created for operator lookup
                // They can fail when we try to lookup operators for things like int32, which is defined globally
                // NOTE: We might want to inject these operators into the target types in the future instead
                if (!member.AllowFailure)
                {
                    // No such member, error
                    member.ReportDiagnostic(diagnostics, builder => builder
                        .WithFormatArgs(member.MemberName, accessed));
                }
                // We still provide a single error symbol
                UnifyWithError(member.MemberType);
                member.CompletionSource.SetResult(ErrorMemberSymbol.Instance);
                return true;
            }
            if (membersWithName.Length == 1)
            {
                // Check visibility
                this.Context.CheckVisibility(member.Locator, membersWithName[0], "member", diagnostics);
                // One member, we know what type the member type is
                var memberType = ((ITypedSymbol)membersWithName[0]).Type;
                // NOTE: There used to be an assignable constraint here
                // But I believe the constraint should strictly stick to its semantics
                // And just provide the member type as is
                UnifyAsserted(member.MemberType, memberType);
                member.CompletionSource.SetResult(membersWithName[0]);
                return true;
            }
            // More than one, the member constraint is fine with multiple members but we don't know the member type
            {
                // All must be functions, otherwise we have bigger problems
                // TODO: Can this assertion fail? Like in a faulty module decl?
                // NOTE: Visibility will be checked by the overload constraint
                Debug.Assert(membersWithName.All(m => m is FunctionSymbol));
                UnifyWithError(member.MemberType);
                var overloadSym = new FunctionGroupSymbol(membersWithName.Cast<FunctionSymbol>().ToImmutableArray());
                member.CompletionSource.SetResult(overloadSym);
                return true;
            }
        }

        // Indexer constraints are trivial, if the receiver is not a type-variable
        bool Indexer()
        {
            if (!this.constraintStore.TryRemove<Indexer>(out var indexer, indexer => !indexer.Receiver.Substitution.IsTypeVariable)) return false;
            var accessed = indexer.Receiver.Substitution;
            // Don't propagate type errors
            if (accessed.IsError)
            {
                UnifyWithError(indexer.ElementType);
                // Best-effort shape approximation
                var errorSymbol = indexer.IsGetter
                    ? ErrorPropertySymbol.CreateIndexerGet(indexer.Indices.Length)
                    : ErrorPropertySymbol.CreateIndexerSet(indexer.Indices.Length);
                indexer.CompletionSource.SetResult(errorSymbol);
                return true;
            }

            // Not a type variable, we can look into members
            var indexers = accessed.AllMembers
                .OfType<PropertySymbol>()
                .Where(p => p.IsIndexer)
                .Select(p => indexer.IsGetter ? p.Getter : p.Setter)
                .OfType<FunctionSymbol>()
                .ToImmutableArray();
            if (indexers.Length == 0)
            {
                indexer.ReportDiagnostic(diagnostics, diag => diag
                    .WithTemplate(indexer.IsGetter
                        ? SymbolResolutionErrors.NoGettableIndexerInType
                        : SymbolResolutionErrors.NoSettableIndexerInType)
                    .WithFormatArgs(accessed));

                UnifyWithError(indexer.ElementType);
                // Best-effort shape approximation
                var errorSymbol = indexer.IsGetter
                    ? ErrorPropertySymbol.CreateIndexerGet(indexer.Indices.Length)
                    : ErrorPropertySymbol.CreateIndexerSet(indexer.Indices.Length);
                indexer.CompletionSource.SetResult(errorSymbol);
                return true;
            }

            // If there is a single indexer, we check visibility
            // This is because in this case overload resolution will skip checking visibility
            if (indexers.Length == 1)
            {
                this.Context.CheckVisibility(indexer.Locator, indexers[0], "indexer", diagnostics);
            }

            if (indexer.IsGetter)
            {
                // Getter, elementType is return type
                this.constraintStore.Add(new Overload(
                    locator: ConstraintLocator.Constraint(indexer),
                    functionName: "operator[]",
                    candidates: OverloadCandidateSet.Create(indexers, indexer.Indices),
                    returnType: indexer.ElementType)
                {
                    // Important, we propagate the completion source
                    CompletionSource = indexer.CompletionSource,
                });
            }
            else
            {
                // Setter
                // We allocate a type var for the return type, but we don't care about it as it's generally just void
                var returnType = this.AllocateTypeVariable();
                this.constraintStore.Add(new Overload(
                    locator: ConstraintLocator.Constraint(indexer),
                    functionName: "operator[]",
                    candidates: OverloadCandidateSet.Create(
                        indexers,
                        // TODO: We pass in null for the value syntax...
                        indexer.Indices.Append(this.Arg(null, indexer.ElementType))),
                    returnType: returnType)
                {
                    // Important, we propagate the completion source
                    CompletionSource = indexer.CompletionSource,
                });
            }
            return true;
        }

        // A callable can be resolved directly, if the called type is not a type-variable
        bool Callable()
        {
            if (!this.constraintStore.TryRemove<Callable>(out var callable, callable => !callable.CalledType.Substitution.IsTypeVariable)) return false;
            var called = callable.CalledType.Substitution;
            if (called.IsError)
            {
                // Don't propagate errors
                UnifyWithError(callable.ReturnType);
                return true;
            }

            // We can now check if it's a function
            // The called thing is either a function, or is a delegate with the appropriate signature
            var functionType = called as FunctionTypeSymbol
                            ?? called.InvokeSignatureType;
            if (functionType is null)
            {
                // Error
                UnifyWithError(callable.ReturnType);
                callable.ReportDiagnostic(diagnostics, diag => diag
                    .WithTemplate(TypeCheckingErrors.CallNonFunction)
                    .WithFormatArgs(called));
                return true;
            }

            // It's a function
            // The inferred return type must be assignable to the return type of the function
            this.Assignable(
                functionType.ReturnType,
                callable.ReturnType,
                ConstraintLocator.Constraint(callable));

            // Check if it has the same number of args
            if (functionType.Parameters.Length != callable.Arguments.Length)
            {
                // Error
                callable.ReportDiagnostic(diagnostics, diag => diag
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(
                        functionType,
                        this.MakeMismatchedFunctionType(callable.Arguments, functionType.ReturnType)));
                return true;
            }

            // Start scoring args
            var candidate = CallCandidate.Create(functionType);
            candidate.Refine(callable.Arguments);

            if (candidate.IsEliminated)
            {
                // Error
                callable.ReportDiagnostic(diagnostics, diag => diag
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(
                        functionType,
                        this.MakeMismatchedFunctionType(callable.Arguments, functionType.ReturnType)));
                return true;
            }

            // We are done
            foreach (var (param, arg) in functionType.Parameters.Zip(callable.Arguments))
            {
                this.AssignParameterToArgument(param.Type, arg);
            }
            return true;
        }

        // If an overload constraint can be advanced, do that
        bool OverloadRefine() =>
            this.constraintStore.TryGet<Overload>(out _, overload => !overload.Candidates.IsWellDefined && overload.Candidates.Refine());

        // If overload constraints are unambiguous, we can resolve them directly
        bool Overload()
        {
            if (!this.constraintStore.TryRemove<Overload>(out var overload, overload => overload.Candidates.IsWellDefined)) return false;
            // Call for safety
            overload.Candidates.Refine();

            var candidates = overload.Candidates.Dominators;
            if (candidates.Length == 0)
            {
                // No such overload, error
                FailOverload(overload);
                // NOTE: If the arguments have an error, we don't report an error here to not cascade errors
                if (overload.Candidates.Arguments.All(a => a.Type.Substitution.IsTypeVariable || !a.Type.Substitution.IsError))
                {
                    overload.ReportDiagnostic(diagnostics, diag => diag
                        .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                        .WithFormatArgs(overload.FunctionName));
                }
                return true;
            }

            if (candidates.Length > 1)
            {
                // Ambiguity, error
                FailOverload(overload);
                // NOTE: If the arguments have an error, we don't report an error here to not cascade errors
                if (overload.Candidates.Arguments.All(a => !a.Type.Substitution.IsError))
                {
                    overload.ReportDiagnostic(diagnostics, diag => diag
                        .WithTemplate(TypeCheckingErrors.AmbiguousOverloadedCall)
                        .WithFormatArgs(overload.FunctionName, string.Join(", ", overload.Candidates)));
                }
                return true;
            }

            // Resolved fine, choose the symbol, which might generic-instantiate it
            var chosen = this.GenericInstantiateIfNeeded(candidates.Single().Data);
            if (overload.Candidates.InitialCandidates.Length != 1)
            {
                // We assume that if the initial candidate count was 1, we already checked visibility
                this.Context.CheckVisibility(overload.Locator, chosen, "overload", diagnostics);
            }
            // Inference
            if (chosen.IsVariadic)
            {
                if (!BinderFacts.TryGetVariadicElementType(chosen.Parameters[^1].Type, out var elementType))
                {
                    // Should not happen
                    throw new InvalidOperationException();
                }
                var nonVariadicPairs = chosen.Parameters
                    .SkipLast(1)
                    .Zip(overload.Candidates.Arguments);
                var variadicPairs = overload.Candidates.Arguments
                    .Skip(chosen.Parameters.Length - 1)
                    .Select(a => (ParameterType: elementType, ArgumentType: a));
                // Non-variadic part
                foreach (var (param, arg) in nonVariadicPairs) this.AssignParameterToArgument(param.Type, arg);
                // Variadic part
                foreach (var (paramType, arg) in variadicPairs) this.AssignParameterToArgument(paramType, arg);
            }
            else
            {
                foreach (var (param, arg) in chosen.Parameters.Zip(overload.Candidates.Arguments))
                {
                    this.AssignParameterToArgument(param.Type, arg);
                }
            }
            // NOTE: This used to be an assignment, but again, I don't think that's in the scope of this constraint
            // In all cases, return type is simple
            UnifyAsserted(overload.ReturnType, chosen.ReturnType);
            // Resolve promise
            overload.CompletionSource.SetResult(chosen);
            return true;
        }

        // As a last resort, we try to drive forward the solver by trying to merge assignable constraints with the same target
        // This is a common situation for things like this:
        //
        // var x = Derived();
        // x = Base();
        //
        // In this case we try to search for the common type of Derived and Base, then assign that
        bool MergeAssignables()
        {
            // First probe, if there are at least 2 such assignables
            var assignablesWithSharedTarget = this.constraintStore
                .Query<Assignable>()
                .GroupBy(a => a.TargetType, SymbolEqualityComparer.AllowTypeVariables)
                .Where(g => g.Count() > 1)
                .FirstOrDefault();
            // No such assignables, we are done
            if (assignablesWithSharedTarget is null) return false;
            // We have at least 2 assignables with the same target, merge them all
            var targetType = assignablesWithSharedTarget.Key;
            var commonType = this.AllocateTypeVariable();
            this.constraintStore.RemoveAll(assignablesWithSharedTarget);
            this.constraintStore.Add(new CommonAncestor(
                // NOTE: Locator is random
                locator: ConstraintLocator.Constraint(assignablesWithSharedTarget.First()),
                commonType: commonType,
                alternativeTypes: assignablesWithSharedTarget.Select(a => a.AssignedType).ToImmutableArray()));
            this.constraintStore.Add(new Assignable(
                // NOTE: Locator is random
                locator: ConstraintLocator.Constraint(assignablesWithSharedTarget.First()),
                targetType: targetType,
                assignedType: commonType));
            return true;
        }

        // As a last-last effort, we assume that a singular assignment means exact matching types
        bool SingleAssignable()
        {
            if (!this.constraintStore.TryRemove<Assignable>(out var assignable, assignable => CanAssign(assignable.TargetType, assignable.AssignedType)))
            {
                return false;
            }
            AssignAsserted(assignable.TargetType, assignable.AssignedType);
            return true;
        }

        // As a last-effort, if we see a common ancestor constraint with a single non-type-var, we
        // assume that the common type is the non-type-var
        // We also substitute all the type-vars with the common type
        bool CommonAncestorSingleNonTypeVar()
        {
            if (this.constraintStore.TryRemove<CommonAncestor>(out var common, common =>
            {
                if (common.AlternativeTypes.Count(t => !t.Substitution.IsTypeVariable) != 1) return false;
                if (common.AlternativeTypes.Count(t => t.Substitution.IsTypeVariable) != common.AlternativeTypes.Length - 1) return false;
                if (common.AlternativeTypes.Any(alt => !CanUnify(alt, common.CommonType))) return false;
                var nonTypeVar = common.AlternativeTypes.First(t => !t.Substitution.IsTypeVariable);
                var typeVars = common.AlternativeTypes.Where(t => t.Substitution.IsTypeVariable);
                if (!CanUnify(common.CommonType, nonTypeVar)) return false;
                return typeVars.All(t => CanUnify(t, nonTypeVar));
            }))
            {
                var nonTypeVar = common.AlternativeTypes.First(t => !t.Substitution.IsTypeVariable);
                var typeVars = common.AlternativeTypes.Where(t => t.Substitution.IsTypeVariable);
                foreach (var typeVar in typeVars) UnifyAsserted(typeVar, nonTypeVar);
                UnifyAsserted(common.CommonType, nonTypeVar);
                return true;
            }
            return false;
        }

        // If the target type of common ancestor is a concrete type, we can try to unify all non-concrete types
        bool CommonAncestorIsGround()
        {
            if (!this.constraintStore.TryRemove<CommonAncestor>(out var common, common => common.CommonType.Substitution.IsGroundType
                                                                                       && common.AlternativeTypes.All(alt => CanUnify(alt, common.CommonType))))
            {
                return false;
            }
            var concreteType = common.CommonType.Substitution;
            foreach (var type in common.AlternativeTypes) UnifyAsserted(type, concreteType);
            return true;
        }
    }

    /// <summary>
    /// Fails the overload constraint by setting the return type to an error type and resolving the promise.
    /// </summary>
    /// <param name="overload">The overload constraint to fail.</param>
    private static void FailOverload(Overload overload)
    {
        UnifyWithError(overload.ReturnType);
        var errorSymbol = new ErrorFunctionSymbol(overload.Candidates.Arguments.Length);
        overload.CompletionSource.SetResult(errorSymbol);
    }

    /// <summary>
    /// Fails all remaining rules in the solver.
    /// </summary>
    /// <param name="diagnostics">Diagnostics to report to.</param>
    private void FailRemainingRules(DiagnosticBag diagnostics)
    {
        var previousStoreSize = this.constraintStore.Count;
        while (true)
        {
            // We unify type variables with the error type
            foreach (var typeVar in this.typeVariables)
            {
                var unwrapped = typeVar.Substitution;
                if (unwrapped is TypeVariable unwrappedTv) UnifyAsserted(unwrappedTv, WellKnownTypes.UninferredType);
            }

            var constraintsToRemove = new List<Constraint>();

            // We can also solve all overload constraints by failing them instantly
            foreach (var overload in this.constraintStore.Query<Overload>())
            {
                FailOverload(overload);
                constraintsToRemove.Add(overload);
            }

            this.constraintStore.RemoveAll(constraintsToRemove);

            // Assume this solves everything
            this.SolveUntilFixpoint(diagnostics);

            // Check for exit condition
            if (previousStoreSize == this.constraintStore.Count) break;
            previousStoreSize = this.constraintStore.Count;
        }

        if (this.constraintStore.Count > 0)
        {
            throw new InvalidOperationException("fallback operation could not solve all constraints");
        }
    }
}
