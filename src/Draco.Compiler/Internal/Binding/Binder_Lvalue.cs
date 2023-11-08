using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;

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
    protected virtual BindingTask<BoundLvalue> BindLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedExpressionSyntax => FromResult(new BoundUnexpectedLvalue(syntax)),
        GroupingExpressionSyntax group => this.BindLvalue(group.Expression, constraints, diagnostics),
        NameExpressionSyntax name => this.BindNameLvalue(name, constraints, diagnostics),
        MemberExpressionSyntax member => this.BindMemberLvalue(member, constraints, diagnostics),
        IndexExpressionSyntax index => this.BindIndexLvalue(index, constraints, diagnostics),
        _ => this.BindIllegalLvalue(syntax, constraints, diagnostics),
    };

    private static BindingTask<BoundLvalue> FromResult(BoundLvalue lvalue) => BindingTask.FromResult(lvalue);

    private BindingTask<BoundLvalue> BindNameLvalue(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        switch (symbol)
        {
        case LocalSymbol local:
            return FromResult(new BoundLocalLvalue(syntax, local));
        case GlobalSymbol global:
            return FromResult(new BoundGlobalLvalue(syntax, global));
        case FieldSymbol:
        case PropertySymbol:
            return FromResult(this.SymbolToLvalue(syntax, symbol, constraints, diagnostics));
        default:
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalLvalue,
                location: syntax?.Location));
            return FromResult(new BoundIllegalLvalue(syntax));
        }
        }
    }

    private async BindingTask<BoundLvalue> BindMemberLvalue(MemberExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var leftTask = this.BindExpression(syntax.Accessed, constraints, diagnostics);
        var memberName = syntax.Member.Text;

        var left = await leftTask;
        var container = ExtractContainer(left);

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
            var memberTask = constraints.Member(
                left.TypeRequired,
                memberName,
                out var memberType,
                syntax);
            var members = await memberTask;

            if (members is ITypedSymbol member)
            {
                // Error, don't cascade
                if (members.IsError)
                {
                    return new BoundIllegalLvalue(syntax);
                }
                if (member is FieldSymbol field)
                {
                    return new BoundFieldLvalue(syntax, left, field);
                }
                if (member is PropertySymbol prop)
                {
                    if (prop.Setter is null)
                    {
                        // TODO
                        throw new NotImplementedException();
                    }
                    return new BoundPropertySetLvalue(syntax, left, prop.Setter, prop.Type);
                }
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalLvalue,
                    location: syntax.Location));
                return new BoundIllegalLvalue(syntax);
            }
            else
            {
                // NOTE: This can happen in case of function with more overloads, but without () after the function name. For example builder.Append
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalFounctionGroupExpression,
                    location: syntax.Location,
                    formatArgs: members.Name));
                return new BoundUnexpectedLvalue(syntax);
            }
        }
    }

    private BindingTask<BoundLvalue> BindIllegalLvalue(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // TODO: Should illegal lvalues contain an expression we still bind?
        // It could result in more errors within the expression, which might be useful
        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.IllegalLvalue,
            location: syntax?.Location));
        return FromResult(new BoundIllegalLvalue(syntax));
    }

    private async BindingTask<BoundLvalue> BindIndexLvalue(IndexExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiverTask = this.BindExpression(syntax.Indexed, constraints, diagnostics);
        var argsTask = syntax.IndexList.Values
            .Select(x => this.BindExpression(x, constraints, diagnostics))
            .ToImmutableArray();

        var receiver = await receiverTask;
        if (receiver is BoundReferenceErrorExpression err)
        {
            return new BoundIllegalLvalue(syntax);
        }

        var receiverType = await ConstraintSolver.Substituted(receiver.TypeRequired);

        // General indexer
        var indexers = receiverType
            .Members
            .OfType<PropertySymbol>()
            .Where(x => x.IsIndexer)
            .Select(x => x.Setter)
            .OfType<FunctionSymbol>()
            .ToImmutableArray();

        var returnType = constraints.AllocateTypeVariable();
        SolverTask<FunctionSymbol> indexerTask;

        if (indexers.Length == 0)
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.NoSettableIndexerInType,
                location: syntax.Location,
                formatArgs: receiverType));
            ConstraintSolver.UnifyAsserted(returnType, IntrinsicSymbols.ErrorType);
            indexerTask = SolverTask.FromResult<FunctionSymbol>(new NoOverloadFunctionSymbol(argsTask.Length + 1));
        }
        else
        {
            indexerTask = constraints.Overload(
                "operator[]",
                indexers,
                argsTask.Append(returnType as object).ToImmutableArray(),
                // NOTE: We don't care about the return type, this is an lvalue
                out _,
                syntax);
        }

        return new BoundIndexSetLvalue(
            syntax,
            receiver,
            await indexerTask,
            await BindingTask.WhenAll(argsTask),
            returnType);
    }

    private BoundLvalue SymbolToLvalue(SyntaxNode syntax, Symbol symbol, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        switch (symbol)
        {
        case GlobalSymbol global:
            return new BoundGlobalLvalue(syntax, global);
        case PropertySymbol prop:
            var setter = GetSetterSymbol(syntax, prop, diagnostics);
            return new BoundPropertySetLvalue(syntax, null, setter, prop.Type);
        default:
            // NOTE: The error is already reported
            return new BoundIllegalLvalue(syntax);
        }
    }
}
