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
    /// Binds the given syntax node to an untyped statement.
    /// </summary>
    /// <param name="syntax">The syntax to bind.</param>
    /// <returns>The untyped statement for <paramref name="syntax"/>.</returns>
    protected UntypedStatement BindStatement(SyntaxNode syntax) => syntax switch
    {
        LabelDeclarationSyntax label => this.BindLabelStatement(label),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedStatement BindLabelStatement(LabelDeclarationSyntax syntax) =>
        throw new NotImplementedException();
}
