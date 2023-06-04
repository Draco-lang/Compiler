using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped lvalue.
    /// </summary>
    /// <param name="syntax">The lvalue to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The untyped lvalue for <paramref name="syntax"/>.</returns>
    protected virtual UntypedLvalue BindLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedExpressionSyntax => new UntypedUnexpectedLvalue(syntax),
        GroupingExpressionSyntax group => this.BindLvalue(group.Expression, constraints, diagnostics),
        NameExpressionSyntax name => this.BindNameLvalue(name, constraints, diagnostics),
        MemberExpressionSyntax member => this.BindMemberLvalue(member, constraints, diagnostics),
        IndexExpressionSyntax index => this.BindIndexLvalue(index, constraints, diagnostics),
        _ => this.BindIllegalLvalue(syntax, constraints, diagnostics),
    };

    private UntypedLvalue BindNameLvalue(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        switch (symbol)
        {
        case UntypedLocalSymbol local:
            return new UntypedLocalLvalue(syntax, local, constraints.GetLocalType(local));
        case GlobalSymbol global:
            return new UntypedGlobalLvalue(syntax, global);
        case FieldSymbol field:
            return new UntypedFieldLvalue(syntax, null, field);
        case PropertySymbol prop:
            var setter = prop.Setter;
            if (setter is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.CannotSetGetOnlyProperty,
                    location: syntax?.Location,
                    prop.FullName));
                setter = new NoOverloadFunctionSymbol(1);
            }
            return new UntypedPropertySetLvalue(syntax, setter, null);
        default:
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalLvalue,
                location: syntax?.Location));
            return new UntypedIllegalLvalue(syntax);
        }
        }
    }

    private UntypedLvalue BindMemberLvalue(MemberExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.BindExpression(syntax.Accessed, constraints, diagnostics);
        var memberName = syntax.Member.Text;
        if (left is UntypedModuleExpression moduleExpr)
        {
            // Module member access
            var module = moduleExpr.Module;
            ImmutableArray<Symbol> members;
            if (BinderFacts.SyntaxMustNotReferenceTypes(syntax)) members = module.StaticMembers
                .Where(m => m.Name == memberName)
                .Where(BinderFacts.IsNonTypeValueSymbol)
                .ToImmutableArray();
            else members = module.StaticMembers
                .Where(m => m.Name == memberName)
                .Where(BinderFacts.IsValueSymbol)
                .ToImmutableArray();
            // Reuse logic from LookupResult
            var result = LookupResult.FromResultSet(members);
            var symbol = result.GetValue(memberName, syntax, diagnostics);
            return this.SymbolToLvalue(syntax, symbol, constraints, diagnostics);
        }
        else if (left is UntypedTypeExpression typeExpr)
        {
            // Type member access
            var type = typeExpr.Type;
            ImmutableArray<Symbol> members;
            if (BinderFacts.SyntaxMustNotReferenceTypes(syntax)) members = type.StaticMembers
                .Where(m => m.Name == memberName)
                .Where(BinderFacts.IsNonTypeValueSymbol)
                .ToImmutableArray();
            else members = type.StaticMembers
                .Where(m => m.Name == memberName)
                .Where(BinderFacts.IsValueSymbol)
                .ToImmutableArray();
            // Reuse logic from LookupResult
            var result = LookupResult.FromResultSet(members);
            var symbol = result.GetValue(memberName, syntax, diagnostics);
            return this.SymbolToLvalue(syntax, symbol, constraints, diagnostics);
        }
        else
        {
            // Value, add constraint
            var promise = constraints.Member(left.TypeRequired, memberName, out var memberType);
            promise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));
            return new UntypedMemberLvalue(syntax, left, memberType, promise);
        }
    }

    private UntypedLvalue BindIllegalLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // TODO: Should illegal lvalues contain an expression we still bind?
        // It could result in more errors within the expression, which might be useful
        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.IllegalLvalue,
            location: syntax?.Location));
        return new UntypedIllegalLvalue(syntax);
    }

    private UntypedLvalue BindIndexLvalue(IndexExpressionSyntax index, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiver = this.BindExpression(index.Indexed, constraints, diagnostics);
        if (receiver is UntypedReferenceErrorExpression err)
        {
            return new UntypedIllegalLvalue(index);
        }
        var args = index.IndexList.Values.Select(x => this.BindExpression(x, constraints, diagnostics)).ToImmutableArray();
        var returnType = constraints.AllocateTypeVariable();
        var promise = constraints.Type(receiver.TypeRequired, () =>
        {
            var indexers = constraints.Unwrap(receiver.TypeRequired).Members.OfType<PropertySymbol>().Where(x => x.IsIndexer).Select(x => x.Setter).OfType<FunctionSymbol>().ToImmutableArray();
            if (indexers.Length == 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.NoSettableIndexerInType,
                    location: index.Location,
                    receiver.ToString()));
                constraints.Unify(returnType, new ErrorTypeSymbol("<error>"));
                return ConstraintPromise.FromResult<FunctionSymbol>(new NoOverloadFunctionSymbol(args.Length + 1));
            }
            var overloaded = constraints.Overload(indexers, args.Select(x => x.TypeRequired).Append(returnType).ToImmutableArray(), out var gotReturnType);
            constraints.Unify(returnType, gotReturnType);
            return overloaded;
        });
        promise.ConfigureDiagnostic(diag => diag
            .WithLocation(index.Location));

        return new UntypedIndexSetLvalue(index, promise, receiver, args, returnType);
    }

    private UntypedLvalue SymbolToLvalue(SyntaxNode syntax, Symbol symbol, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        switch (symbol)
        {
        case FieldSymbol field:
            return new UntypedFieldLvalue(syntax, null, field);
        case PropertySymbol prop:
            var setter = prop.Setter;
            if (setter is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.CannotSetGetOnlyProperty,
                    location: syntax?.Location,
                    prop.FullName));
                setter = new NoOverloadFunctionSymbol(1);
            }
            return new UntypedPropertySetLvalue(syntax, setter, null);
        default:
            // NOTE: The error is already reported
            return new UntypedIllegalLvalue(syntax);
        }
    }
}
