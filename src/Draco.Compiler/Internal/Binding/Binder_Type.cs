using System;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.UntypedTree;

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
        MemberTypeSyntax member => this.BindMemberType(member, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Symbol BindNameType(NameTypeSyntax syntax, DiagnosticBag diagnostics) =>
        this.LookupTypeSymbol(syntax.Name.Text, syntax, diagnostics);

    private Symbol BindMemberType(MemberTypeSyntax syntax, DiagnosticBag diagnostics)
    {
        var left = this.BindType(syntax.Accessed, diagnostics);
        var memberName = syntax.Member.Text;
        if (left.IsError)
        {
            // Error, don't cascade
            return left;
        }
        else if (left is ModuleSymbol module)
        {
            // TODO
            throw new NotImplementedException();
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
    }
}
