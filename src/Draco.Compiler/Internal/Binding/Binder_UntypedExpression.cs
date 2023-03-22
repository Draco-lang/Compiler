using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
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
    protected UntypedExpression BindExpression(SyntaxNode syntax, ConstraintBag constraints, DiagnosticBag diagnostics) => syntax switch
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
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindStatementExpression(StatementExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // We just desugar stmt; into { stmt; }
        var stmt = this.BindStatement(syntax.Statement, constraints, diagnostics);
        return new UntypedBlockExpression(
            syntax: syntax,
            locals: ImmutableArray<UntypedLocalSymbol>.Empty,
            statements: ImmutableArray.Create(stmt),
            value: UntypedUnitExpression.Default);
    }

    private UntypedExpression BindLiteralExpression(LiteralExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) =>
        new UntypedLiteralExpression(syntax, syntax.Literal.Value);

    private UntypedExpression BindStringExpression(StringExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) =>
        new UntypedStringExpression(syntax, syntax.Parts.Select(p => this.BindStringPart(p, constraints, diagnostics)).ToImmutableArray());

    private UntypedStringPart BindStringPart(StringPartSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) => syntax switch
    {
        TextStringPartSyntax text => new UntypedStringText(syntax, text.Content.ValueText!),
        InterpolationStringPartSyntax interpolation => new UntypedStringInterpolation(
            syntax,
            this.BindExpression(interpolation.Expression, constraints, diagnostics)),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        if (symbol.IsError) return new UntypedReferenceErrorExpression(syntax, symbol);
        return symbol switch
        {
            ParameterSymbol param => new UntypedParameterExpression(syntax, param),
            UntypedLocalSymbol local => new UntypedLocalExpression(syntax, local, constraints.LocalReference(local, syntax)),
            GlobalSymbol global => new UntypedGlobalExpression(syntax, global),
            FunctionSymbol func => new UntypedFunctionExpression(syntax, ConstraintPromise.FromResult(func), func.Type),
            _ => throw new InvalidOperationException(),
        };
    }

    private UntypedExpression BindBlockExpression(BlockExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
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

    private UntypedExpression BindGotoExpression(GotoExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var target = (LabelSymbol)this.BindLabel(syntax.Target, constraints, diagnostics);
        return new UntypedGotoExpression(syntax, target);
    }

    private UntypedExpression BindReturnExpression(ReturnExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var value = syntax.Value is null
            ? UntypedUnitExpression.Default
            : this.BindExpression(syntax.Value, constraints, diagnostics);

        // Return type constraint
        var containingFunction = (FunctionSymbol?)this.ContainingSymbol;
        Debug.Assert(containingFunction is not null);
        constraints.Return(value, containingFunction, syntax);

        return new UntypedReturnExpression(syntax, value);
    }

    private UntypedExpression BindIfExpression(IfExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var condition = this.BindExpression(syntax.Condition, constraints, diagnostics);
        var then = this.BindExpression(syntax.Then, constraints, diagnostics);
        var @else = syntax.Else is null
            ? UntypedTreeFactory.UnitExpression()
            : this.BindExpression(syntax.Else.Expression, constraints, diagnostics);

        constraints.IsCondition(condition);
        var resultType = constraints.CommonType(then, @else);

        return new UntypedIfExpression(syntax, condition, then, @else, resultType);
    }

    private UntypedExpression BindWhileExpression(WhileExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);

        var condition = binder.BindExpression(syntax.Condition, constraints, diagnostics);
        var then = binder.BindExpression(syntax.Then, constraints, diagnostics);

        constraints.IsCondition(condition);
        constraints.IsUnit(then);

        var continueLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "continue");
        var breakLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "break");

        return new UntypedWhileExpression(syntax, condition, then, continueLabel, breakLabel);
    }

    private UntypedExpression BindCallExpression(CallExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var method = this.BindExpression(syntax.Function, constraints, diagnostics);
        var args = syntax.ArgumentList.Values
            .Select(arg => this.BindExpression(arg, constraints, diagnostics))
            .ToImmutableArray();

        var returnType = constraints.CallFunction(method, args, syntax);

        return new UntypedCallExpression(syntax, method, args, returnType);
    }

    private UntypedExpression BindUnaryExpression(UnaryExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // Get the unary operator symbol
        var operatorName = UnaryOperatorSymbol.GetUnaryOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
        var operand = this.BindExpression(syntax.Operand, constraints, diagnostics);

        var (symbolPromise, resultType) = constraints.CallUnaryOperator(operatorSymbol, operand, syntax);

        return new UntypedUnaryExpression(syntax, symbolPromise, operand, resultType);
    }

    private UntypedExpression BindBinaryExpression(BinaryExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        if (syntax.Operator.Kind == TokenKind.Assign)
        {
            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            constraints.IsAssignable(left, right, syntax);

            return new UntypedAssignmentExpression(syntax, null, left, right);
        }
        else if (syntax.Operator.Kind is TokenKind.KeywordAnd or TokenKind.KeywordOr)
        {
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            constraints.IsBool(left);
            constraints.IsBool(right);

            return syntax.Operator.Kind == TokenKind.KeywordAnd
                ? new UntypedAndExpression(syntax, left, right)
                : new UntypedOrExpression(syntax, left, right);
        }
        else if (SyntaxFacts.TryGetOperatorOfCompoundAssignment(syntax.Operator.Kind, out var nonCompound))
        {
            // Get the binary operator symbol
            var operatorName = BinaryOperatorSymbol.GetBinaryOperatorName(nonCompound);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);

            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            var (symbolPromise, resultType) = constraints.CallBinaryOperator(operatorSymbol, left, right, syntax);
            constraints.IsAssignable(left, resultType, syntax);

            return new UntypedAssignmentExpression(syntax, symbolPromise, left, right);
        }
        else
        {
            // Get the binary operator symbol
            var operatorName = BinaryOperatorSymbol.GetBinaryOperatorName(syntax.Operator.Kind);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            var (symbolPromise, resultType) = constraints.CallBinaryOperator(operatorSymbol, left, right, syntax);

            return new UntypedBinaryExpression(syntax, symbolPromise, left, right, resultType);
        }
    }

    private UntypedExpression BindRelationalExpression(RelationalExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
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
        ConstraintBag constraints,
        DiagnosticBag diagnostics)
    {
        // Get the comparison operator symbol
        var operatorName = ComparisonOperatorSymbol.GetComparisonOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
        var right = this.BindExpression(syntax.Right, constraints, diagnostics);

        // NOTE: We know it must be bool, no need to pass it on to comparison
        var symbolPromise = constraints.CallComparisonOperator(operatorSymbol, prev, right, syntax);

        return new UntypedComparison(syntax, symbolPromise, right);
    }
}
