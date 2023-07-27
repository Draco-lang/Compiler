using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private bool ApplyRules(DiagnosticBag diagnostics)
    {
        if (this.TryDequeue<SameTypeConstraint>(out var sameType))
        {
            this.HandleRule(sameType, diagnostics);
            return true;
        }

        if (this.TryDequeue<AssignableConstraint>(
            out var assignable,
            a => !a.TargetType.Substitution.IsTypeVariable && !a.AssignedType.Substitution.IsTypeVariable))
        {
            this.HandleRule(assignable, diagnostics);
            return true;
        }

        if (this.TryDequeue<CommonTypeConstraint>(
            out var common,
            c => c.AlternativeTypes.All(t => !t.Substitution.IsTypeVariable)))
        {
            this.HandleRule(common, diagnostics);
            return true;
        }

        if (this.TryDequeue<MemberConstraint>(out var member, m => !m.Accessed.Substitution.IsTypeVariable))
        {
            this.HandleRule(member, diagnostics);
            return true;
        }

        if (this.TryDequeue<AwaitConstraint<UntypedExpression>>(out var wait, w => w.Awaited()))
        {
            this.HandleRule(wait);
            return true;
        }

        if (this.TryDequeue<AwaitConstraint<Symbol>>(out var wait2, w => w.Awaited()))
        {
            this.HandleRule(wait2);
            return true;
        }

        if (this.TryDequeue<AwaitConstraint<TypeSymbol>>(out var wait3, w => w.Awaited()))
        {
            this.HandleRule(wait3);
            return true;
        }

        if (this.TryDequeue<AwaitConstraint<IConstraintPromise<FunctionSymbol>>>(out var wait4, w => w.Awaited()))
        {
            this.HandleRule(wait4);
            return true;
        }

        foreach (var overload in this.Enumerate<OverloadConstraint>())
        {
            this.HandleRule(overload, diagnostics);
            if (!overload.Promise.IsResolved) continue;

            this.Remove(overload);
            return true;
        }

        foreach (var call in this.Enumerate<CallConstraint>(c => !c.CalledType.Substitution.IsTypeVariable))
        {
            this.HandleRule(call, diagnostics);
            if (!call.Promise.IsResolved) continue;

            this.Remove(call);
            return true;
        }

        if (this.TryDequeue<AssignableConstraint>(out assignable, a => a.TargetType.Substitution.IsTypeVariable))
        {
            // See if there are other assignments
            var assignmentsWithSameTarget = this
                .Enumerate<AssignableConstraint>(a => ReferenceEquals(assignable.TargetType, a.TargetType))
                .ToList();
            if (assignmentsWithSameTarget.Count == 0)
            {
                // No, assume same type
                this.Unify(assignable.TargetType, assignable.AssignedType);
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

            // TODO: We forget diag info...
            this.CommonType(commonType, alternatives);
            // New assignable
            this.Assignable(assignable.TargetType, commonType);
            return true;
        }

        return false;
    }

    private void FailRemainingRules()
    {
        foreach (var constraint in this.constraints) this.FailRemainingRule(constraint);
    }

    private void FailRemainingRule(IConstraint constraint)
    {
        switch (constraint)
        {
        case CallConstraint call:
            this.FailRule(call);
            break;
        case OverloadConstraint overload:
            this.FailRule(overload);
            break;
        }
    }

    private void HandleRule(SameTypeConstraint constraint, DiagnosticBag diagnostics)
    {
        for (var i = 1; i < constraint.Types.Length; ++i)
        {
            if (!this.Unify(constraint.Types[0], constraint.Types[i]))
            {
                // Type-mismatch
                constraint.Diagnostic
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(constraint.Types[0].Substitution, constraint.Types[i].Substitution);
                constraint.Promise.Fail(default, diagnostics);
                return;
            }
        }

        // Successful unification
        constraint.Promise.Resolve(default);
    }

    private void HandleRule(AssignableConstraint constraint, DiagnosticBag diagnostics)
    {
        if (!IsBaseOf(constraint.TargetType, constraint.AssignedType))
        {
            // Type-mismatch
            constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.TypeMismatch)
                .WithFormatArgs(constraint.TargetType.Substitution, constraint.AssignedType.Substitution);
            constraint.Promise.Fail(default, diagnostics);
            return;
        }

        // Ok
        constraint.Promise.Resolve(default);
    }

    private void HandleRule(CommonTypeConstraint constraint, DiagnosticBag diagnostics)
    {
        foreach (var type in constraint.AlternativeTypes)
        {
            if (constraint.AlternativeTypes.All(t => IsBaseOf(type, t)))
            {
                // Found a good common type
                this.Unify(constraint.CommonType, type);
                constraint.Promise.Resolve(default);
                return;
            }
        }

        // No common type
        constraint.Diagnostic
            .WithTemplate(TypeCheckingErrors.NoCommonType)
            .WithFormatArgs(string.Join(", ", constraint.AlternativeTypes));
        this.Unify(constraint.CommonType, IntrinsicSymbols.ErrorType);
        constraint.Promise.Fail(default, diagnostics);
    }

    private void HandleRule(MemberConstraint constraint, DiagnosticBag diagnostics)
    {
        var accessed = constraint.Accessed.Substitution;
        // We can't advance on type variables
        if (accessed.IsTypeVariable)
        {
            throw new InvalidOperationException("rule handling for member constraint called prematurely");
        }

        // Not a type variable, we can look into members
        var membersWithName = accessed.InstanceMembers
            .Where(m => m.Name == constraint.MemberName)
            .ToImmutableArray();

        if (membersWithName.Length == 0)
        {
            // No such member, error
            constraint.Diagnostic
                .WithTemplate(SymbolResolutionErrors.MemberNotFound)
                .WithFormatArgs(constraint.MemberName, accessed);
            // We still provide a single error symbol
            var errorSymbol = new UndefinedMemberSymbol();
            this.Unify(constraint.MemberType, IntrinsicSymbols.ErrorType);
            constraint.Promise.Fail(errorSymbol, diagnostics);
            return;
        }

        if (membersWithName.Length == 1)
        {
            // One member, we know what type the member type is
            this.Unify(((ITypedSymbol)membersWithName[0]).Type, constraint.MemberType);
            constraint.Promise.Resolve(membersWithName[0]);
            return;
        }

        // More than one, the member constraint is fine with multiple members but we don't know the member type
        {
            // All must be functions, otherwise we have bigger problems
            // TODO: Can this assertion fail? Like in a faulty module decl?
            Debug.Assert(membersWithName.All(m => m is FunctionSymbol));
            this.Unify(constraint.MemberType, IntrinsicSymbols.ErrorType);
            var overload = new OverloadSymbol(membersWithName.Cast<FunctionSymbol>().ToImmutableArray());
            constraint.Promise.Resolve(overload);
        }
    }

    private void HandleRule<T>(AwaitConstraint<T> constraint)
    {
        // Wait until resolved
        if (!constraint.Awaited())
        {
            throw new InvalidOperationException("rule handling for await constraint called prematurely");
        }

        // We can resolve the awaited promise
        var mappedValue = constraint.Map();

        // Resolve this promise
        constraint.Promise.Resolve(mappedValue);
    }

    private void HandleRule(OverloadConstraint constraint, DiagnosticBag diagnostics)
    {
        var functionName = constraint.Candidates[0].Name;
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
            this.Unify(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            // Best-effort shape approximation
            var errorSymbol = new NoOverloadFunctionSymbol(constraint.Arguments.Length);
            constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                .WithFormatArgs(functionName);
            constraint.Promise.Fail(errorSymbol, diagnostics);
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
            // NOTE: Unification won't always be correct, especially not when subtyping arises
            // In all cases, return type is simple
            this.Unify(constraint.ReturnType, chosen.ReturnType);
            // Resolve promise
            constraint.Promise.Resolve(chosen);
        }
        else
        {
            // Best-effort shape approximation
            this.Unify(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            var errorSymbol = new NoOverloadFunctionSymbol(constraint.Arguments.Length);
            constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.AmbiguousOverloadedCall)
                .WithFormatArgs(functionName, string.Join(", ", dominatingCandidates));
            constraint.Promise.Fail(errorSymbol, diagnostics);
        }
    }

    private void HandleRule(CallConstraint constraint, DiagnosticBag diagnostics)
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
            this.Unify(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.CallNonFunction)
                .WithFormatArgs(called);
            constraint.Promise.Fail(default, diagnostics);
            return;
        }

        // It's a function
        // We can merge the return type
        this.Unify(constraint.ReturnType, functionType.ReturnType);

        // Check if it has the same number of args
        if (functionType.Parameters.Length != constraint.Arguments.Length)
        {
            // Error
            this.Unify(constraint.ReturnType, IntrinsicSymbols.ErrorType);
            constraint.Diagnostic
                .WithTemplate(TypeCheckingErrors.TypeMismatch)
                .WithFormatArgs(
                    functionType,
                    MakeMismatchedFunctionType(constraint.Arguments, functionType.ReturnType));
            constraint.Promise.Fail(default, diagnostics);
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
                this.Unify(constraint.ReturnType, IntrinsicSymbols.ErrorType);
                constraint.Diagnostic
                    .WithTemplate(TypeCheckingErrors.TypeMismatch)
                    .WithFormatArgs(
                        functionType,
                        MakeMismatchedFunctionType(constraint.Arguments, functionType.ReturnType));
                constraint.Promise.Fail(default, diagnostics);
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

    private void FailRule(OverloadConstraint constraint)
    {
        this.Unify(constraint.ReturnType, IntrinsicSymbols.ErrorType);
        var errorSymbol = new NoOverloadFunctionSymbol(constraint.Arguments.Length);
        constraint.Promise.Fail(errorSymbol, null);
    }

    private void FailRule(CallConstraint constraint)
    {
        this.Unify(constraint.ReturnType, IntrinsicSymbols.ErrorType);
        constraint.Promise.Fail(default, null);
    }
}
