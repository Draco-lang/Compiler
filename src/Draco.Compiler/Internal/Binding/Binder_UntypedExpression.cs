using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped expression.
    /// </summary>
    /// <param name="syntax">The syntax to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The untyped expression for <paramref name="syntax"/>.</returns>
    protected virtual UntypedExpression BindExpression(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedExpressionSyntax => new UntypedUnexpectedExpression(syntax),
        GroupingExpressionSyntax grp => this.BindExpression(grp.Expression, constraints, diagnostics),
        StatementExpressionSyntax stmt => this.BindStatementExpression(stmt, constraints, diagnostics),
        LiteralExpressionSyntax lit => this.BindLiteralExpression(lit, constraints, diagnostics),
        StringExpressionSyntax str => this.BindStringExpression(str, constraints, diagnostics),
        NameExpressionSyntax name => this.BindNameExpression(name, constraints, diagnostics),
        BlockExpressionSyntax block => this.BindBlockExpression(block, constraints, diagnostics),
        GotoExpressionSyntax @goto => this.BindGotoExpression(@goto, constraints, diagnostics),
        ReturnExpressionSyntax @return => this.BindReturnExpression(@return, constraints, diagnostics),
        IfExpressionSyntax @if => this.BindIfExpression(@if, constraints, diagnostics),
        WhileExpressionSyntax @while => this.BindWhileExpression(@while, constraints, diagnostics),
        CallExpressionSyntax call => this.BindCallExpression(call, constraints, diagnostics),
        UnaryExpressionSyntax ury => this.BindUnaryExpression(ury, constraints, diagnostics),
        BinaryExpressionSyntax bin => this.BindBinaryExpression(bin, constraints, diagnostics),
        RelationalExpressionSyntax rel => this.BindRelationalExpression(rel, constraints, diagnostics),
        MemberExpressionSyntax maccess => this.BindMemberExpression(maccess, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindStatementExpression(StatementExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // We just desugar stmt; into { stmt; }
        var stmt = this.BindStatement(syntax.Statement, constraints, diagnostics);
        return new UntypedBlockExpression(
            syntax: syntax,
            locals: ImmutableArray<UntypedLocalSymbol>.Empty,
            statements: ImmutableArray.Create(stmt),
            value: UntypedUnitExpression.Default);
    }

    private UntypedExpression BindLiteralExpression(LiteralExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new UntypedLiteralExpression(syntax, syntax.Literal.Value);

    private UntypedExpression BindStringExpression(StringExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new UntypedStringExpression(syntax, syntax.Parts.Select(p => this.BindStringPart(p, constraints, diagnostics)).ToImmutableArray());

    private UntypedStringPart BindStringPart(StringPartSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedStringPartSyntax => new UntypedUnexpectedStringPart(syntax),
        TextStringPartSyntax text => new UntypedStringText(syntax, text.Content.ValueText!),
        InterpolationStringPartSyntax interpolation => new UntypedStringInterpolation(
            syntax,
            this.BindExpression(interpolation.Expression, constraints, diagnostics)),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        return this.SymbolToExpression(syntax, symbol, constraints, diagnostics);
    }

    private UntypedExpression BindBlockExpression(BlockExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .ToImmutableArray();
        var statements = syntax.Statements
            .Select(s => binder.BindStatement(s, constraints, diagnostics))
            .ToImmutableArray();
        var value = syntax.Value is null
            ? UntypedUnitExpression.Default
            : binder.BindExpression(syntax.Value, constraints, diagnostics);
        return new UntypedBlockExpression(syntax, locals, statements, value);
    }

    private UntypedExpression BindGotoExpression(GotoExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var target = (LabelSymbol)this.BindLabel(syntax.Target, constraints, diagnostics);
        return new UntypedGotoExpression(syntax, target);
    }

    private UntypedExpression BindReturnExpression(ReturnExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var value = syntax.Value is null
            ? UntypedUnitExpression.Default
            : this.BindExpression(syntax.Value, constraints, diagnostics);

        this.ConstraintReturnType(syntax, value, constraints);

        return new UntypedReturnExpression(syntax, value);
    }

    private UntypedExpression BindIfExpression(IfExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var condition = this.BindExpression(syntax.Condition, constraints, diagnostics);
        // Condition must be bool
        constraints
            .SameType(IntrinsicSymbols.Bool, condition.TypeRequired)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));

        var then = this.BindExpression(syntax.Then, constraints, diagnostics);
        var @else = syntax.Else is null
            ? UntypedUnitExpression.Default
            : this.BindExpression(syntax.Else.Expression, constraints, diagnostics);

        // Then and else must be compatible types
        var resultType = constraints
            .CommonType(then.TypeRequired, @else.TypeRequired)
            .ConfigureDiagnostic(diag =>
            {
                // The location will point at the else value, assuming that the latter expression is
                // the offending one
                // If there is no else clause, we just point at the then clause
                diag.WithLocation(syntax.Else is null
                    ? ExtractValueSyntax(syntax.Then).Location
                    : ExtractValueSyntax(syntax.Else.Expression).Location);
                // If there is an else clause, we annotate the then clause as related info
                diag.WithRelatedInformation(
                    format: "the other branch is inferred to be {0}",
                    formatArgs: then.TypeRequired,
                    location: ExtractValueSyntax(syntax.Then).Location);
            }).Result;

        return new UntypedIfExpression(syntax, condition, then, @else, resultType);
    }

    private UntypedExpression BindWhileExpression(WhileExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);

        var condition = binder.BindExpression(syntax.Condition, constraints, diagnostics);
        // Condition must be bool
        constraints
            .SameType(IntrinsicSymbols.Bool, condition.TypeRequired)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));

        var then = binder.BindExpression(syntax.Then, constraints, diagnostics);
        // Body must be unit
        constraints
            .SameType(IntrinsicSymbols.Unit, then.TypeRequired)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(ExtractValueSyntax(syntax.Then).Location));

        // Resolve labels
        var continueLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "continue");
        var breakLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "break");

        return new UntypedWhileExpression(syntax, condition, then, continueLabel, breakLabel);
    }

    private UntypedExpression BindCallExpression(CallExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var method = this.BindExpression(syntax.Function, constraints, diagnostics);
        var args = syntax.ArgumentList.Values
            .Select(arg => this.BindExpression(arg, constraints, diagnostics))
            .ToImmutableArray();

        // TODO: We need a proper Call constraint here that actually handles overloads
        // For that we need to extract the promise of method groups or something
        // This could be a member expression, a simple function group expression, or something else
        // which would be an indirect call
        throw new NotImplementedException();

        /*var returnType = constraints
            .Call(method.TypeRequired, args.Select(arg => arg.TypeRequired))
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location))
            .Result;*/
        // return new UntypedCallExpression(syntax, method, args, returnType);
    }

    private UntypedExpression BindUnaryExpression(UnaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Get the unary operator symbol
        var operatorName = FunctionSymbol.GetUnaryOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
        var operand = this.BindExpression(syntax.Operand, constraints, diagnostics);

        // Resolve symbol overload
        var (symbolPromise, callSite) = constraints.Overload(operatorSymbol);
        symbolPromise.ConfigureDiagnostic(diag => diag
            .WithLocation(syntax.Operator.Location));
        // Call the operator
        var resultType = constraints
            .Call(callSite, new[] { operand.TypeRequired })
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location))
            .Result;

        return new UntypedUnaryExpression(syntax, symbolPromise, operand, resultType);
    }

    private UntypedExpression BindBinaryExpression(BinaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (syntax.Operator.Kind == TokenKind.Assign)
        {
            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Right must be assignable to left
            constraints
                .Assignable(left.Type, right.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Location));

            return new UntypedAssignmentExpression(syntax, null, left, right);
        }
        else if (syntax.Operator.Kind is TokenKind.KeywordAnd or TokenKind.KeywordOr)
        {
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Both left and right must be bool
            constraints
                .SameType(IntrinsicSymbols.Bool, left.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Left.Location));
            constraints
                .SameType(IntrinsicSymbols.Bool, right.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Right.Location));

            return syntax.Operator.Kind == TokenKind.KeywordAnd
                ? new UntypedAndExpression(syntax, left, right)
                : new UntypedOrExpression(syntax, left, right);
        }
        else if (SyntaxFacts.TryGetOperatorOfCompoundAssignment(syntax.Operator.Kind, out var nonCompound))
        {
            // Get the binary operator symbol
            var operatorName = FunctionSymbol.GetBinaryOperatorName(nonCompound);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);

            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Resolve symbol overload
            var (symbolPromise, callSite) = constraints.Overload(operatorSymbol);
            symbolPromise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Operator.Location));
            // Call the operator
            var resultType = constraints
                .Call(callSite, new[] { left.Type, right.TypeRequired })
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Location))
                .Result;
            // The result of the binary operator must be assignable to the left-hand side
            // For example, a + b in the form of a += b means that a + b has to result in a type
            // that is assignable to a, hence the extra constraint
            constraints
                .Assignable(left.Type, resultType)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Location));

            return new UntypedAssignmentExpression(syntax, symbolPromise, left, right);
        }
        else
        {
            // Get the binary operator symbol
            var operatorName = FunctionSymbol.GetBinaryOperatorName(syntax.Operator.Kind);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Resolve symbol overload
            var (symbolPromise, callSite) = constraints.Overload(operatorSymbol);
            symbolPromise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Operator.Location));
            // Call the operator
            var resultType = constraints
                .Call(callSite, new[] { left.TypeRequired, right.TypeRequired })
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Location))
                .Result;

            return new UntypedBinaryExpression(syntax, symbolPromise, left, right, resultType);
        }
    }

    private UntypedExpression BindRelationalExpression(RelationalExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var first = this.BindExpression(syntax.Left, constraints, diagnostics);
        var comparisons = ImmutableArray.CreateBuilder<UntypedComparison>();
        var prev = first;
        foreach (var comparisonSyntax in syntax.Comparisons)
        {
            var comparison = this.BindComparison(prev, comparisonSyntax, constraints, diagnostics);
            prev = comparison.Next;
            comparisons.Add(comparison);
        }
        return new UntypedRelationalExpression(syntax, first, comparisons.ToImmutable());
    }

    private UntypedComparison BindComparison(
        UntypedExpression prev,
        ComparisonElementSyntax syntax,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
        // Get the comparison operator symbol
        var operatorName = FunctionSymbol.GetComparisonOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
        var right = this.BindExpression(syntax.Right, constraints, diagnostics);

        // NOTE: We know it must be bool, no need to pass it on to comparison
        // Resolve symbol overload
        var (symbolPromise, callSite) = constraints.Overload(operatorSymbol);
        symbolPromise.ConfigureDiagnostic(diag => diag
            .WithLocation(syntax.Operator.Location));
        // Call the operator
        var resultType = constraints
            .Call(callSite, new[] { prev.TypeRequired, right.TypeRequired })
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location))
            .Result;
        // For safety, we assume it has to be bool
        constraints
            .SameType(IntrinsicSymbols.Bool, resultType)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Operator.Location));

        return new UntypedComparison(syntax, symbolPromise, right);
    }

    private UntypedExpression BindMemberExpression(MemberExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.BindExpression(syntax.Accessed, constraints, diagnostics);
        var memberName = syntax.Member.Text;
        if (left is UntypedReferenceErrorExpression err)
        {
            // Error, don't cascade
            return new UntypedReferenceErrorExpression(syntax, err.Symbol);
        }
        else if (left is UntypedModuleExpression moduleExpr)
        {
            // Module member access
            var module = moduleExpr.Module;
            var members = module.Members
                .Where(m => m.Name == memberName)
                .Where(BinderFacts.IsValueSymbol)
                .ToImmutableArray();
            // Reuse logic from LookupResult
            var result = LookupResult.FromResultSet(members);
            var symbol = result.GetValue(memberName, syntax, diagnostics);
            return this.SymbolToExpression(syntax, symbol, constraints, diagnostics);
        }
        else
        {
            // Value, add constraint
            var (promise, type) = constraints.Member(left.TypeRequired, memberName);
            promise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));
            return new UntypedMemberExpression(syntax, left, promise, type);
        }
    }

    private UntypedExpression SymbolToExpression(SyntaxNode syntax, Symbol symbol, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (symbol.IsError) return new UntypedReferenceErrorExpression(syntax, symbol);
        switch (symbol)
        {
        case Symbol when symbol.IsError:
            return new UntypedReferenceErrorExpression(syntax, symbol);
        case ModuleSymbol module:
            // NOTE: Hack, see the node above this method definition
            this.BindModuleSyntaxToSymbol(syntax, module);
            return new UntypedModuleExpression(syntax, module);
        case ParameterSymbol param:
            return new UntypedParameterExpression(syntax, param);
        case UntypedLocalSymbol local:
            return new UntypedLocalExpression(syntax, local, constraints.GetLocal(local));
        case GlobalSymbol global:
            return new UntypedGlobalExpression(syntax, global);
        case FunctionSymbol func:
            return new UntypedFunctionGroupExpression(syntax, ImmutableArray.Create(func));
        case OverloadSymbol overload:
            return new UntypedFunctionGroupExpression(syntax, overload.Functions);
        default:
            throw new InvalidOperationException();
        }
    }

    private static ExpressionSyntax ExtractValueSyntax(ExpressionSyntax syntax) => syntax switch
    {
        IfExpressionSyntax @if => ExtractValueSyntax(@if.Then),
        WhileExpressionSyntax @while => ExtractValueSyntax(@while.Then),
        BlockExpressionSyntax block => block.Value is null ? block : ExtractValueSyntax(block.Value),
        _ => syntax,
    };
}
