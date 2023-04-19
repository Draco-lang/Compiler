using System;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to a type symbol.
    /// </summary>
    /// <param name="syntax">The type to bind.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The looked up type symbol for <paramref name="syntax"/>.</returns>
    internal virtual Symbol BindType(TypeSyntax syntax, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedTypeSyntax => new UndefinedTypeSymbol("<error>"),
        NameTypeSyntax name => this.BindNameType(name, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Symbol BindNameType(NameTypeSyntax syntax, DiagnosticBag diagnostics) =>
        this.LookupTypeSymbol(syntax.Name.Text, syntax, diagnostics);
}
