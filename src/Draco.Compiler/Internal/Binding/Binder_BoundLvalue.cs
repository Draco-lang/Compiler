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
    /// Binds the given untyped lvalue to a bound lvalue.
    /// </summary>
    /// <param name="lvalue">The untyped lvalue to bind.</param>
    /// <returns>The bound lvalue for <paramref name="lvalue"/>.</returns>
    protected BoundLvalue BindExpression(UntypedLvalue lvalue) => lvalue switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(lvalue)),
    };
}
