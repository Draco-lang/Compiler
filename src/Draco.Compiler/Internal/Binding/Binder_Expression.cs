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
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    /// <summary>
    /// Binds the given untyped expression to a bound expression.
    /// </summary>
    /// <param name="expression">The untyped expression to bind.</param>
    /// <returns>The bound expression for <paramref name="expression"/>.</returns>
    protected BoundExpression BindExpression(UntypedExpression expression) => expression switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(expression)),
    };
}
