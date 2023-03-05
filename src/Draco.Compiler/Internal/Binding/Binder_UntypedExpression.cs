using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
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
        LiteralExpressionSyntax lit => this.BindLiteralExpression(lit, constraints, diagnostics),
        NameExpressionSyntax name => this.BindNameExpression(name, constraints, diagnostics),
        IfExpressionSyntax @if => this.BindIfExpression(@if, constraints, diagnostics),
        UnaryExpressionSyntax ury => this.BindUnaryExpression(ury, constraints, diagnostics),
        BinaryExpressionSyntax bin => this.BindBinaryExpression(bin, constraints, diagnostics),
        RelationalExpressionSyntax rel => this.BindRelationalExpression(rel, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindLiteralExpression(LiteralExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) =>
        new UntypedLiteralExpression(syntax, syntax.Literal.Value);

    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var lookup = this.LookupValueSymbol(syntax.Name.Text, syntax);
        if (!lookup.FoundAny)
        {
            // TODO
            throw new NotImplementedException();
        }
        if (lookup.Symbols.Count > 1)
        {
            // TODO: Multiple symbols, potental overloading
            throw new NotImplementedException();
        }
        return lookup.Symbols[0] switch
        {
            ParameterSymbol param => new UntypedParameterExpression(syntax, param),
            _ => throw new InvalidOperationException(),
        };
    }

    private UntypedExpression BindIfExpression(IfExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var condition = this.BindExpression(syntax.Condition, constraints, diagnostics);
        var then = this.BindExpression(syntax.Then, constraints, diagnostics);
        var @else = syntax.Else is null
            ? UntypedTreeFactory.UnitExpression()
            : this.BindExpression(syntax.Else.Expression, constraints, diagnostics);
        return new UntypedIfExpression(syntax, condition, then, @else);
    }

    private UntypedExpression BindUnaryExpression(UnaryExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // Get the unary operator symbol
        var operatorName = UnaryOperatorSymbol.GetUnaryOperatorName(syntax.Operator.Kind);
        var operatorLookup = this.LookupValueSymbol(operatorName, syntax);
        if (!operatorLookup.FoundAny || operatorLookup.Symbols.Count > 1)
        {
            // TODO: Handle overload or illegal
            throw new NotImplementedException();
        }
        var operatorSymbol = (UnaryOperatorSymbol)operatorLookup.Symbols[0];
        var operand = this.BindExpression(syntax.Operand, constraints, diagnostics);
        return new UntypedUnaryExpression(syntax, operatorSymbol, operand);
    }

    private UntypedExpression BindBinaryExpression(BinaryExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        if (syntax.Operator.Kind == TokenKind.Assign)
        {
            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);
            return new UntypedAssignmentExpression(syntax, left, right);
        }
        else if (SyntaxFacts.IsCompoundAssignmentOperator(syntax.Operator.Kind))
        {
            // TODO: Compound assignment
            throw new NotImplementedException();
        }
        else
        {
            // Get the binary operator symbol
            var operatorName = BinaryOperatorSymbol.GetBinaryOperatorName(syntax.Operator.Kind);
            var operatorLookup = this.LookupValueSymbol(operatorName, syntax);
            if (!operatorLookup.FoundAny || operatorLookup.Symbols.Count > 1)
            {
                // TODO: Handle overload or illegal
                throw new NotImplementedException();
            }
            var operatorSymbol = (BinaryOperatorSymbol)operatorLookup.Symbols[0];
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);
            return new UntypedBinaryExpression(syntax, operatorSymbol, left, right);
        }
    }

    private UntypedExpression BindRelationalExpression(RelationalExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var first = this.BindExpression(syntax.Left, constraints, diagnostics);
        var comparisons = syntax.Comparisons
            .Select(cmp => this.BindComparison(cmp, constraints, diagnostics))
            .ToImmutableArray();
        return new UntypedRelationalExpression(syntax, first, comparisons);
    }

    private UntypedComparison BindComparison(ComparisonElementSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // Get the comparison operator symbol
        var operatorName = ComparisonOperatorSymbol.GetComparisonOperatorName(syntax.Operator.Kind);
        var operatorLookup = this.LookupValueSymbol(operatorName, syntax);
        if (!operatorLookup.FoundAny || operatorLookup.Symbols.Count > 1)
        {
            // TODO: Handle overload or illegal
            throw new NotImplementedException();
        }
        var operatorSymbol = (ComparisonOperatorSymbol)operatorLookup.Symbols[0];
        var right = this.BindExpression(syntax.Right, constraints, diagnostics);
        return new UntypedComparison(syntax, operatorSymbol, right);
    }
}
