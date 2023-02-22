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
        NameExpressionSyntax name => this.BindNameExpression(name),
        IfExpressionSyntax @if => this.BindIfExpression(@if),
        RelationalExpressionSyntax rel => this.BindRelationalExpression(rel),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };
    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax)
    {
        // TODO: Look up name properly
        var symbol = (Symbol?)null ?? throw new NotImplementedException();
        // TODO: Based on symbol return something
        throw new NotImplementedException();
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

    private UntypedExpression BindRelationalExpression(RelationalExpressionSyntax syntax)
    {
        var first = this.BindExpression(syntax.Left);
        var comparisons = ImmutableArray.CreateBuilder<UntypedComparison>();
        foreach (var cmpSyntax in syntax.Comparisons)
        {
            // TODO: Look up operator properly
            var symbol = (ComparisonOperatorSymbol?)null ?? throw new NotImplementedException();
            var right = this.BindExpression(cmpSyntax.Right);
            comparisons.Add(new UntypedComparison(cmpSyntax, symbol, right));
        }
        return new UntypedRelationalExpression(syntax, first, comparisons.ToImmutable());
    }
}
