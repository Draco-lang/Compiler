using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal abstract partial class UntypedBinder
{
    protected UntypedStatement BindStatement(SyntaxNode node) => node switch
    {
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
