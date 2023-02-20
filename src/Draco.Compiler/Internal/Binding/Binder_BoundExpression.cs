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
    /// Binds the given untyped expression to a bound expression.
    /// </summary>
    /// <param name="expression">The untyped expression to bind.</param>
    /// <returns>The bound expression for <paramref name="expression"/>.</returns>
    protected BoundExpression BindExpression(UntypedExpression expression) => expression switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(expression)),
    };
}
