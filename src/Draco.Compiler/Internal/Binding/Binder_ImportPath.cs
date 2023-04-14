using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given import path, resolving it to a symbol.
    /// </summary>
    /// <param name="syntax">The path syntax to resolve.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The symbol that the import path pointed to.</returns>
    internal virtual Symbol BindImportPath(ImportPathSyntax syntax, DiagnosticBag diagnostics) => syntax switch
    {
        RootImportPathSyntax root => this.BindRootImportPath(root, diagnostics),
        MemberImportPathSyntax mem => this.BindMemberImportPath(mem, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Symbol BindRootImportPath(RootImportPathSyntax syntax, DiagnosticBag diagnostics) =>
        // Simple lookup from here
        this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);

    private Symbol BindMemberImportPath(MemberImportPathSyntax syntax, DiagnosticBag diagnostics)
    {
        var parent = this.BindImportPath(syntax.Accessed, diagnostics);
        if (parent.IsError)
        {
            // Don't cascade errors
            return parent;
        }
        // Look up in parent
        var membersWithName = parent.Members
            .Where(m => m.Name == syntax.Member.Text)
            .ToList();
        if (membersWithName.Count == 1)
        {
            // Simply return this
            return membersWithName[0];
        }
        else
        {
            // TODO: 0 or multiple should give some nice error message
            throw new NotImplementedException();
        }
    }
}
