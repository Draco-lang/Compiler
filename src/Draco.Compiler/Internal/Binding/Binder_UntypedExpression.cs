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
    /// <returns>The untyped expression for <paramref name="syntax"/>.</returns>
    protected UntypedExpression BindExpression(SyntaxNode syntax) => syntax switch
    {
        LiteralExpressionSyntax lit => this.BindLiteralExpression(lit),
        NameExpressionSyntax name => this.BindNameExpression(name),
        IfExpressionSyntax @if => this.BindIfExpression(@if),
        UnaryExpressionSyntax ury => this.BindUnaryExpression(ury),
        RelationalExpressionSyntax rel => this.BindRelationalExpression(rel),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindLiteralExpression(LiteralExpressionSyntax syntax) =>
        new UntypedLiteralExpression(syntax, syntax.Literal.Value);

    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax)
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

    private UntypedExpression BindIfExpression(IfExpressionSyntax syntax)
    {
        var condition = this.BindExpression(syntax.Condition);
        var then = this.BindExpression(syntax.Then);
        var @else = syntax.Else is null
            ? UntypedTreeFactory.UnitExpression()
            : this.BindExpression(syntax.Else.Expression);
        return new UntypedIfExpression(syntax, condition, then, @else);
    }

    private UntypedExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
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
        var operand = this.BindExpression(syntax.Operand);
        return new UntypedUnaryExpression(syntax, operatorSymbol, operand);
    }

    private UntypedExpression BindRelationalExpression(RelationalExpressionSyntax syntax)
    {
        var first = this.BindExpression(syntax.Left);
        var comparisons = syntax.Comparisons
            .Select(this.BindComparison)
            .ToImmutableArray();
        return new UntypedRelationalExpression(syntax, first, comparisons);
    }

    private UntypedComparison BindComparison(ComparisonElementSyntax syntax)
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
        var right = this.BindExpression(syntax.Right);
        return new UntypedComparison(syntax, operatorSymbol, right);
    }
}
