using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Chr.Constraints;
using Draco.Chr.Rules;
using Draco.Compiler.Api.Diagnostics;
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
    private static IEnumerable<Rule> ConstructRules(DiagnosticBag diagnostics) => [
        // Trivial same-type constraint, unify all
        Simplification(typeof(Same))
            .Body((ConstraintStore store, Same same) =>
            {
                for (var i = 1; i < same.Types.Length; ++i)
                {
                    if (Unify(same.Types[0], same.Types[i])) continue;

                    // Type-mismatch
                    same.ReportDiagnostic(diagnostics, builder => builder
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
                assignable.ReportDiagnostic(diagnostics, diag => diag
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
                    member.ReportDiagnostic(diagnostics, builder => builder
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
                    // Assignable constraint will resolve the type
                    store.Add(new Assignable(
                        Locator: ConstraintLocator.Constraint(member),
                        TargetType: member.MemberType,
                        AssignedType: memberType));
                    member.CompletionSource.SetResult(membersWithName[0]);
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
    ];
}
