using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Implemented by classes that can provide binders for the syntax nodes.
/// </summary>
internal interface IBinderProvider
{
    /// <summary>
    /// The diagnostic bag to write diagnostics to.
    /// </summary>
    public DiagnosticBag DiagnosticBag { get; }

    /// <summary>
    /// Retrieves the <see cref="Binder"/> for a given syntax node.
    /// </summary>
    /// <param name="syntax">The syntax node to retrieve the binder for.</param>
    /// <returns>The binder that corresponds to <paramref name="syntax"/>.</returns>
    public Binder GetBinder(SyntaxNode syntax);

    /// <summary>
    /// Retrieves the <see cref="Binder"/> for a given symbol definition.
    /// </summary>
    /// <param name="symbol">The symbol to retrieve the binder for.</param>
    /// <returns>The binder that corresponds to <paramref name="symbol"/>.</returns>
    public Binder GetBinder(Symbol symbol);
}
