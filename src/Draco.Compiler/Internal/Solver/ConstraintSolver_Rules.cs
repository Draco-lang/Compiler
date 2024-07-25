using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver.Constraints;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Chr.Rules.RuleFactory;

namespace Draco.Compiler.Internal.Solver;

internal sealed partial class ConstraintSolver
{
    private IEnumerable<Rule> ConstructRules(DiagnosticBag diagnostics) => [
        // Trivial same-type constraint, unify all
        Simplification(typeof(Same))
            .Body((ConstraintStore store, Same same) =>
            {
                for (var i = 1; i < same.Types.Length; ++i)
                {
                    if (Unify(same.Types[0], same.Types[i])) continue;
                    // Type-mismatch
                    same
                        .ReportDiagnostic(diagnostics, builder => builder
                        .WithFormatArgs(same.Types[0].Substitution, same.Types[i].Substitution));
                    break;
                }
            }),

        // Assignable can be resolved directly, if both types are ground-types
        Simplification(typeof(Assignable))
            .Guard((Assignable assignable) => assignable.TargetType.IsGroundType
                                           && assignable.AssignedType.IsGroundType)
            .Body((ConstraintStore store, Assignable assignable) =>
            {
                if (SymbolEqualityComparer.Default.IsBaseOf(assignable.TargetType, assignable.AssignedType))
                {
                    // Ok
                    return;
                }
                // Error
                assignable
                    .ReportDiagnostic(diagnostics, diag => diag
                    .WithFormatArgs(assignable.TargetType.Substitution, assignable.AssignedType.Substitution));
            }),

        // If all types are ground-types, common-type constraints are trivial
        Simplification(typeof(CommonAncestor))
            .Guard((CommonAncestor common) => common.AlternativeTypes.All(t => t.IsGroundType))
            .Body((ConstraintStore store, CommonAncestor common) =>
            {
                foreach (var type in common.AlternativeTypes)
                {
                    if (!common.AlternativeTypes.All(t => SymbolEqualityComparer.Default.IsBaseOf(type, t))) continue;
                    // Found a good common type
                    UnifyAsserted(common.CommonType, type);
                    return;
                }
                // No common type found
                common.ReportDiagnostic(diagnostics, builder => builder
                    .WithFormatArgs(string.Join(", ", common.AlternativeTypes)));
                // Stop cascading uninferred type
                UnifyAsserted(common.CommonType, WellKnownTypes.ErrorType);
            }),

        // Member constraints are trivial, if the receiver is a ground-type
        Simplification(typeof(Member))
            .Guard((Member member) => member.Receiver.IsGroundType)
            .Body((ConstraintStore store, Member member) =>
            {
                var accessed = member.Receiver.Substitution;
                // Don't propagate type errors
                if (accessed.IsError)
                {
                    Unify(member.MemberType, WellKnownTypes.ErrorType);
                    member.CompletionSource.SetResult(UndefinedMemberSymbol.Instance);
                    return;
                }

                // Not a type variable, we can look into members
                var membersWithName = accessed.Members
                    .Where(m => m.Name == member.MemberName)
                    .ToImmutableArray();
                if (membersWithName.Length == 0)
                {
                    // No such member, error
                    member
                        .ReportDiagnostic(diagnostics, builder => builder
                        .WithFormatArgs(member.MemberName, accessed));
                    // We still provide a single error symbol
                    UnifyAsserted(member.MemberType, WellKnownTypes.ErrorType);
                    member.CompletionSource.SetResult(UndefinedMemberSymbol.Instance);
                    return;
                }
                if (membersWithName.Length == 1)
                {
                    // One member, we know what type the member type is
                    var memberType = ((ITypedSymbol)membersWithName[0]).Type;
                    // NOTE: There used to be an assignable constraint here
                    // But I believe the constraint should strictly stick to its semantics
                    // And just provide the member type as is
                    UnifyAsserted(member.MemberType, memberType);
                    return;
                }
                // More than one, the member constraint is fine with multiple members but we don't know the member type
                {
                    // All must be functions, otherwise we have bigger problems
                    // TODO: Can this assertion fail? Like in a faulty module decl?
                    Debug.Assert(membersWithName.All(m => m is FunctionSymbol));
                    UnifyAsserted(member.MemberType, WellKnownTypes.ErrorType);
                    var overload = new OverloadSymbol(membersWithName.Cast<FunctionSymbol>().ToImmutableArray());
                    member.CompletionSource.SetResult(overload);
                }
            }),

        // If overload constraints are unambiguous, we can resolve them directly
        Simplification(typeof(Overload))
            .Guard((Overload overload) => overload.Candidates.IsWellDefined)
            .Body((ConstraintStore store, Overload overload) =>
            {
                // Call for safety
                overload.Candidates.Refine();

                var candidates = overload.Candidates.Dominators;
                if (candidates.Length == 0)
                {
                    // Could not resolve, error
                    UnifyAsserted(overload.ReturnType, WellKnownTypes.ErrorType);
                    // Best-effort shape approximation
                    var errorSymbol = new NoOverloadFunctionSymbol(overload.Candidates.Arguments.Length);
                    overload
                        .ReportDiagnostic(diagnostics, diag => diag
                        .WithTemplate(TypeCheckingErrors.NoMatchingOverload)
                        .WithFormatArgs(overload.FunctionName));
                    overload.CompletionSource.SetResult(errorSymbol);
                    return;
                }

                if (candidates.Length > 1)
                {
                    // Ambiguity, error
                    // Best-effort shape approximation
                    UnifyAsserted(overload.ReturnType, WellKnownTypes.ErrorType);
                    var errorSymbol = new NoOverloadFunctionSymbol(overload.Candidates.Arguments.Length);
                    overload
                        .ReportDiagnostic(diagnostics, diag => diag
                        .WithTemplate(TypeCheckingErrors.AmbiguousOverloadedCall)
                        .WithFormatArgs(overload.FunctionName, string.Join(", ", overload.Candidates)));
                    overload.CompletionSource.SetResult(errorSymbol);
                    return;
                }

                // Resolved fine, choose the symbol, which might generic-instantiate it
                var chosen = this.GenericInstantiateIfNeeded(candidates.Single().Data);
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
            }),

        // If an overload constraint can be advanced, do that
        Propagation(typeof(Overload))
            .Guard((Overload overload) => overload.Candidates.Refine()),

        // TODO: Case when we have multiple overloaded candidates but it's all well-defined

        // As a last resort, we try to drive forward the solver by trying to merge assignable constraints with the same target
        // This is a common situation for things like this:
        //
        // var x = Derived();
        // x = Base();
        //
        // In this case we try to search for the common type of Derived and Base, then assign that
        Simplification(typeof(Assignable), typeof(Assignable))
            .Guard((Assignable a1, Assignable a2) =>
                SymbolEqualityComparer.AllowTypeVariables.Equals(a1.TargetType, a2.TargetType))
            .Body((ConstraintStore store, Assignable a1, Assignable a2) =>
            {
                var targetType = a1.TargetType;
                var commonType = this.AllocateTypeVariable();
                this.CommonType(commonType, [a1.AssignedType, a2.AssignedType], ConstraintLocator.Constraint(a2));
                this.Assignable(targetType, commonType, ConstraintLocator.Constraint(a2));
            }),

        // As a last-last effort, we assume that a singular assignment means exact matching types
        Simplification(typeof(Assignable))
            .Body((ConstraintStore store, Assignable assignable) =>
            {
                // TODO: Is asserted correct here?
                // Maybe just for type-variables?
                UnifyAsserted(assignable.TargetType, assignable.AssignedType);
            }),
    ];
}
