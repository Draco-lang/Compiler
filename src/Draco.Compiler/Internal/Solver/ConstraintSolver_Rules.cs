using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private bool ApplyRules(DiagnosticBag? diagnostics)
    {
        if (this.TryDequeue<SameTypeConstraint>(out var sameType))
        {
            this.HandleRule(sameType, diagnostics);
            return true;
        }

        if (this.TryDequeue<AssignableConstraint>(out var assignable, a => a.TargetType.IsGroundType && a.AssignedType.IsGroundType))
        {
            this.HandleRule(assignable, diagnostics);
            return true;
        }

        if (this.TryDequeue<CommonTypeConstraint>(out var common, c => c.AlternativeTypes.All(t => t.IsGroundType)))
        {
            this.HandleRule(common, diagnostics);
            return true;
        }

        if (this.TryDequeue<MemberConstraint>(out var member, m => !m.Accessed.Substitution.IsTypeVariable))
        {
            this.HandleRule(member, diagnostics);
            return true;
        }

        // NOTE: Await constraints used to be here, maybe yield here?

        foreach (var overload in this.Enumerate<OverloadConstraint>())
        {
            this.HandleRule(overload, diagnostics);
            if (!overload.CompletionSource.IsCompleted) continue;

            this.Remove(overload);
            return true;
        }

        foreach (var call in this.Enumerate<CallConstraint>(c => !c.CalledType.Substitution.IsTypeVariable))
        {
            this.HandleRule(call, diagnostics);
            if (!call.CompletionSource.IsCompleted) continue;

            this.Remove(call);
            return true;
        }

        if (this.TryDequeue<AssignableConstraint>(out assignable))
        {
            // See if there are other assignments
            var assignmentsWithSameTarget = this
                .Enumerate<AssignableConstraint>(a => SymbolEqualityComparer.AllowTypeVariables.Equals(assignable.TargetType, a.TargetType))
                .ToList();
            if (assignmentsWithSameTarget.Count == 0)
            {
                // No, assume same type
                UnifyAsserted(assignable.TargetType, assignable.AssignedType);
                return true;
            }

            // There are multiple constraints targeting the same type
            // Remove them
            foreach (var a in assignmentsWithSameTarget) this.Remove(a);

            // Create a common-type constraint for them
            var commonType = this.AllocateTypeVariable();
            var alternatives = assignmentsWithSameTarget
                .Select(a => a.AssignedType)
                .Append(assignable.AssignedType)
                .ToImmutableArray();

            // New assignable
            this.CommonType(commonType, alternatives, ConstraintLocator.Constraint(assignable));
            this.Assignable(assignable.TargetType, commonType, ConstraintLocator.Constraint(assignable));
            return true;
        }

        foreach (var common2 in this.Enumerate<CommonTypeConstraint>())
        {
            var commonTypeVars = common2.AlternativeTypes
                .Where(t => t.Substitution.IsTypeVariable)
                .ToImmutableArray();
            var commonNonTypeVars = common2.AlternativeTypes
                .Where(t => !t.Substitution.IsTypeVariable)
                .ToImmutableArray();
            if (commonNonTypeVars.Length != 1) continue;

            // NOTE: We do NOT remove the constraint, will be resolved in a future iteration
            // Only one non-type-var, the rest are type variables
            var nonTypeVar = commonNonTypeVars[0];
            foreach (var tv in commonTypeVars) UnifyAsserted(tv, nonTypeVar);
            return true;
        }

        return false;
    }

    private void FailRemainingRules()
    {
        // We unify type variables with the error type
        foreach (var typeVar in this.typeVariables)
        {
            var unwrapped = typeVar.Substitution;
            if (unwrapped is TypeVariable unwrappedTv) UnifyAsserted(unwrappedTv, IntrinsicSymbols.UninferredType);
        }

        while (this.constraints.Count > 0)
        {
            // Apply rules once
            if (!this.ApplyRules(null)) break;
        }
    }

    private void HandleRule(SameTypeConstraint constraint, DiagnosticBag? diagnostics)
    {
        for (var i = 1; i < constraint.Types.Length; ++i)
        {
            if (!Unify(constraint.Types[0], constraint.Types[i]))
            {
                // Type-mismatch
                diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(constraint.Types[0].Substitution, constraint.Types[i].Substitution))
                    .Build());
                constraint.CompletionSource.SetResult(default);
                return;
            }
        }

        // Successful unification
        constraint.CompletionSource.SetResult(default);
    }

    private void HandleRule(AssignableConstraint constraint, DiagnosticBag? diagnostics)
    {
        if (!SymbolEqualityComparer.Default.IsBaseOf(constraint.TargetType, constraint.AssignedType))
        {
            // Type-mismatch
            diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                .WithTemplate(TypeCheckingErrors.TypeMismatch)
                .WithFormatArgs(constraint.TargetType.Substitution, constraint.AssignedType.Substitution))
                .Build());
            constraint.CompletionSource.SetResult(default);
            return;
        }

        // Ok
        constraint.CompletionSource.SetResult(default);
    }

    private void HandleRule(CommonTypeConstraint constraint, DiagnosticBag? diagnostics)
    {
        foreach (var type in constraint.AlternativeTypes)
        {
            if (constraint.AlternativeTypes.All(t => SymbolEqualityComparer.Default.IsBaseOf(type, t)))
            {
                // Found a good common type
                UnifyAsserted(constraint.CommonType, type);
                constraint.CompletionSource.SetResult(default);
                return;
            }
        }

        // No common type
        diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
            .WithTemplate(TypeCheckingErrors.NoCommonType)
            .WithFormatArgs(string.Join(", ", constraint.AlternativeTypes)))
            .Build());
        UnifyAsserted(constraint.CommonType, IntrinsicSymbols.ErrorType);
        constraint.CompletionSource.SetResult(default);
    }

    private void HandleRule(MemberConstraint constraint, DiagnosticBag? diagnostics)
    {
        var accessed = constraint.Accessed.Substitution;
        // We can't advance on type variables
        if (accessed.IsTypeVariable)
        {
            throw new InvalidOperationException("rule handling for member constraint called prematurely");
        }

        // Don't propagate type errors
        if (accessed.IsError)
        {
            Unify(constraint.MemberType, IntrinsicSymbols.ErrorType);
            constraint.CompletionSource.SetResult(UndefinedMemberSymbol.Instance);
            return;
        }

        // Not a type variable, we can look into members
        var membersWithName = accessed.InstanceMembers
            .Where(m => m.Name == constraint.MemberName)
            .ToImmutableArray();

        if (membersWithName.Length == 0)
        {
            // No such member, error
            diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                .WithTemplate(SymbolResolutionErrors.MemberNotFound)
                .WithFormatArgs(constraint.MemberName, accessed))
                .Build());
            // We still provide a single error symbol
            UnifyAsserted(constraint.MemberType, IntrinsicSymbols.ErrorType);
            constraint.CompletionSource.SetResult(UndefinedMemberSymbol.Instance);
            return;
        }

        if (membersWithName.Length == 1)
        {
            // One member, we know what type the member type is
            var memberType = ((ITypedSymbol)membersWithName[0]).Type;
            var assignablePromise = this.Assignable(constraint.MemberType, memberType, ConstraintLocator.Constraint(constraint));
            constraint.CompletionSource.SetResult(membersWithName[0]);
            return;
        }

        // More than one, the member constraint is fine with multiple members but we don't know the member type
        {
            // All must be functions, otherwise we have bigger problems
            // TODO: Can this assertion fail? Like in a faulty module decl?
            Debug.Assert(membersWithName.All(m => m is FunctionSymbol));
            UnifyAsserted(constraint.MemberType, IntrinsicSymbols.ErrorType);
            var overload = new OverloadSymbol(membersWithName.Cast<FunctionSymbol>().ToImmutableArray());
            constraint.CompletionSource.SetResult(overload);
        }
    }

    private void HandleRule(OverloadConstraint constraint, DiagnosticBag? diagnostics)
    {
        var functionName = constraint.Name;
        var functionsWithMatchingArgc = constraint.Candidates
            .Where(f => MatchesParameterCount(f, constraint.Arguments.Length))
            .ToList();
        var maxArgc = functionsWithMatchingArgc
            .Select(f => f.Parameters.Length)
            .Append(0)
            .Max();
        var candidates = functionsWithMatchingArgc
            .Select(f => new OverloadCandidate(f, new(maxArgc)))
            .ToList();

        while (true)
        {
            var changed = RefineOverloadScores(candidates, constraint.Arguments, out var wellDefined);
            if (wellDefined) break;
            if (candidates.Count <= 1) break;
            if (!changed) return;
        }

        // We have all candidates well-defined, find the absolute dominator
        if (candidates.Count == 0)
        {
            UnifyAsserted(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            // Best-effort shape approximation
            var errorSymbol = new NoOverloadFunctionSymbol(constraint.Arguments.Length);
            diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                .WithFormatArgs(functionName))
                .Build());
            constraint.CompletionSource.SetResult(errorSymbol);
            return;
        }

        // We have one or more, find the max dominator
        var dominatingCandidates = GetDominatingCandidates(candidates);
        if (dominatingCandidates.Length == 1)
        {
            // Resolved fine, choose the symbol, which might generic-instantiate it
            var chosen = this.ChooseSymbol(dominatingCandidates[0]);

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
                    .Zip(constraint.Arguments);
                var variadicPairs = constraint.Arguments
                    .Skip(chosen.Parameters.Length - 1)
                    .Select(a => (ParameterType: elementType, ArgumentType: a));
                // Non-variadic part
                foreach (var (param, arg) in nonVariadicPairs) this.UnifyParameterWithArgument(param.Type, arg);
                // Variadic part
                foreach (var (paramType, arg) in variadicPairs) this.UnifyParameterWithArgument(paramType, arg);
            }
            else
            {
                foreach (var (param, arg) in chosen.Parameters.Zip(constraint.Arguments))
                {
                    this.UnifyParameterWithArgument(param.Type, arg);
                }
            }
            // In all cases, return type is simple, it's an assignment
            var returnTypePromise = this.Assignable(constraint.ReturnType, chosen.ReturnType, ConstraintLocator.Constraint(constraint));
            // Resolve promise
            constraint.CompletionSource.SetResult(chosen);
        }
        else
        {
            // Best-effort shape approximation
            UnifyAsserted(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            var errorSymbol = new NoOverloadFunctionSymbol(constraint.Arguments.Length);
            diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                .WithTemplate(TypeCheckingErrors.AmbiguousOverloadedCall)
                .WithFormatArgs(functionName, string.Join(", ", dominatingCandidates)))
                .Build());
            constraint.CompletionSource.SetResult(errorSymbol);
        }
    }

    private void HandleRule(CallConstraint constraint, DiagnosticBag? diagnostics)
    {
        var called = constraint.CalledType.Substitution;
        // We can't advance on type variables
        if (called.IsTypeVariable)
        {
            throw new InvalidOperationException("rule handling for call constraint called prematurely");
        }

        if (called.IsError)
        {
            // Don't propagate errors
            this.FailRule(constraint);
            return;
        }

        // We can now check if it's a function
        if (called is not FunctionTypeSymbol functionType)
        {
            // Error
            UnifyAsserted(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                .WithTemplate(TypeCheckingErrors.CallNonFunction)
                .WithFormatArgs(called))
                .Build());
            constraint.CompletionSource.SetResult(default);
            return;
        }

        // It's a function
        // We can merge the return type
        UnifyAsserted(constraint.ReturnType, functionType.ReturnType);

        // Check if it has the same number of args
        if (functionType.Parameters.Length != constraint.Arguments.Length)
        {
            // Error
            UnifyAsserted(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                .WithTemplate(TypeCheckingErrors.TypeMismatch)
                .WithFormatArgs(
                    functionType,
                    MakeMismatchedFunctionType(constraint.Arguments, functionType.ReturnType)))
                .Build());
            constraint.CompletionSource.SetResult(default);
            return;
        }

        // Start scoring args
        var score = new CallScore(functionType.Parameters.Length);
        while (true)
        {
            var changed = AdjustScore(functionType, constraint.Arguments, score);
            if (score.HasZero)
            {
                // Error
                UnifyAsserted(constraint.ReturnType, IntrinsicSymbols.ErrorType);
                diagnostics?.Add(constraint.BuildDiagnostic(diag => diag
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(
                        functionType,
                        MakeMismatchedFunctionType(constraint.Arguments, functionType.ReturnType)))
                    .Build());
                constraint.CompletionSource.SetResult(default);
                return;
            }
            if (score.IsWellDefined) break;
            if (!changed) return;
        }

        // We are done
        foreach (var (param, arg) in functionType.Parameters.Zip(constraint.Arguments))
        {
            this.UnifyParameterWithArgument(param.Type, arg);
        }
    }

    private void FailRule(CallConstraint constraint)
    {
        UnifyAsserted(constraint.ReturnType, IntrinsicSymbols.ErrorType);
        constraint.CompletionSource.SetResult(default);
    }
}
