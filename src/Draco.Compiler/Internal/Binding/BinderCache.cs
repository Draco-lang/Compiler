using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Declarations;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Responsible for caching the binders for syntax nodes and declarations.
/// </summary>
internal sealed class BinderCache
{
    /// <summary>
    /// Retrieves a <see cref="Binder"/> for the given declaration.
    /// </summary>
    /// <param name="declaration">The declaration to retrieve the binder for.</param>
    /// <returns>The binder for <paramref name="declaration"/>.</returns>
    public Binder Get(Declaration declaration) =>
        throw new NotImplementedException();

    /// <summary>
    /// Retrieves a <see cref="Binder"/> for the given syntax node.
    /// </summary>
    /// <param name="syntax">The syntax node to retrieve the binder for.</param>
    /// <returns>The binder for <paramref name="syntax"/>.</returns>
    public Binder Get(SyntaxNode syntax) =>
        throw new NotImplementedException();
}
