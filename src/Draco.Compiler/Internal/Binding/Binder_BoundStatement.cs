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
    /// Binds the given untyped statement to a bound statement.
    /// </summary>
    /// <param name="statement">The untyped statement to bind.</param>
    /// <returns>The bound statement for <paramref name="statement"/>.</returns>
    protected BoundStatement BindStatement(UntypedStatement statement) => statement switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(statement)),
    };
}
