using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
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
        IfExpressionSyntax @if => this.BindIfExpression(@if),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedExpression BindIfExpression(IfExpressionSyntax syntax)
    {
        var condition = this.BindExpression(syntax.Condition);
        var then = this.BindExpression(syntax.Then);
        var @else = syntax.Else is null
            ? UntypedTreeFactory.UnitExpression()
            : this.BindExpression(syntax.Else.Expression);
        return this.BindIfExpression(syntax, condition, then, @else);
    }

    private UntypedExpression BindIfExpression(
        IfExpressionSyntax syntax,
        UntypedExpression condition,
        UntypedExpression then,
        UntypedExpression @else) => new UntypedIfExpression(syntax, condition, then, @else);
}
