using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
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
    protected virtual BoundLvalue BindLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedExpressionSyntax => new BoundUnexpectedLvalue(syntax),
        GroupingExpressionSyntax group => this.BindLvalue(group.Expression, constraints, diagnostics),
        NameExpressionSyntax name => this.BindNameLvalue(name, constraints, diagnostics),
        MemberExpressionSyntax member => this.BindMemberLvalue(member, constraints, diagnostics),
        IndexExpressionSyntax index => this.BindIndexLvalue(index, constraints, diagnostics),
        _ => this.BindIllegalLvalue(syntax, constraints, diagnostics),
    };

    private BoundLvalue BindNameLvalue(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        switch (symbol)
        {
        case UntypedLocalSymbol local:
            return new BoundLocalLvalue(syntax, local, constraints.GetLocalType(local));
        case GlobalSymbol global:
            return new BoundGlobalLvalue(syntax, global);
        case FieldSymbol:
            return this.SymbolToLvalue(syntax, symbol, constraints, diagnostics);
        case PropertySymbol:
            return this.SymbolToLvalue(syntax, symbol, constraints, diagnostics);
        default:
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalLvalue,
                location: syntax?.Location));
            return new BoundIllegalLvalue(syntax);
        }
        }
    }

    private BoundLvalue BindMemberLvalue(MemberExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.BindExpression(syntax.Accessed, constraints, diagnostics);
        var memberName = syntax.Member.Text;

        Symbol? container = left is BoundModuleExpression untypedModule
            ? untypedModule.Module
            : (left as BoundTypeExpression)?.Type;

        if (container is not null)
        {
            Func<Symbol, bool> pred = BinderFacts.SyntaxMustNotReferenceTypes(syntax)
                ? BinderFacts.IsNonTypeValueSymbol
                : BinderFacts.IsValueSymbol;

            var members = container.StaticMembers
                .Where(m => m.Name == memberName && m.Visibility != Api.Semantics.Visibility.Private)
                .Where(pred)
                .ToImmutableArray();

            var result = LookupResult.FromResultSet(members);
            var symbol = result.GetValue(memberName, syntax, diagnostics);
            return this.SymbolToLvalue(syntax, symbol, constraints, diagnostics);
        }
        else
        {
            // Value, add constraint
            var promise = constraints.Member(left.TypeRequired, memberName, out var memberType, syntax);
            return new BoundMemberLvalue(syntax, left, promise, memberType);
        }
    }

    private BoundLvalue BindIllegalLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // TODO: Should illegal lvalues contain an expression we still bind?
        // It could result in more errors within the expression, which might be useful
        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.IllegalLvalue,
            location: syntax?.Location));
        return new BoundIllegalLvalue(syntax);
    }

    private BoundLvalue BindIndexLvalue(IndexExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiver = this.BindExpression(syntax.Indexed, constraints, diagnostics);
        if (receiver is BoundReferenceErrorExpression err)
        {
            return new BoundIllegalLvalue(syntax);
        }
        var args = syntax.IndexList.Values
            .Select(x => this.BindExpression(x, constraints, diagnostics))
            .ToImmutableArray();
        var returnType = constraints.AllocateTypeVariable();
        var promise = constraints.Substituted(receiver.TypeRequired, () =>
        {
            var receiverType = receiver.TypeRequired.Substitution;

            // General indexer
            var indexers = receiverType
                .Members
                .OfType<PropertySymbol>()
                .Where(x => x.IsIndexer)
                .Select(x => x.Setter)
                .OfType<FunctionSymbol>()
                .ToImmutableArray();
            if (indexers.Length == 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.NoSettableIndexerInType,
                    location: syntax.Location,
                    formatArgs: receiverType));
                constraints.UnifyAsserted(returnType, IntrinsicSymbols.ErrorType);
                return ConstraintPromise.FromResult<FunctionSymbol>(new NoOverloadFunctionSymbol(args.Length + 1));
            }
            var overloaded = constraints.Overload(
                "operator[]",
                indexers,
                args.Append(returnType as object).ToImmutableArray(),
                // NOTE: We don't care about the return type, this is an lvalue
                out _,
                syntax);
            return overloaded;
        }, syntax).Unwrap();

        return new BoundIndexSetLvalue(syntax, receiver, promise, args, returnType);
    }

    private BoundLvalue SymbolToLvalue(SyntaxNode syntax, Symbol symbol, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        switch (symbol)
        {
        case GlobalSymbol global:
            return new BoundGlobalLvalue(syntax, global);
        case PropertySymbol prop:
            var setter = GetSetterSymbol(syntax, prop, diagnostics);
            return new BoundPropertySetLvalue(syntax, null, setter);
        default:
            // NOTE: The error is already reported
            return new BoundIllegalLvalue(syntax);
        }
    }
}
