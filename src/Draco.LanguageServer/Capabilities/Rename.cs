using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal partial class DracoLanguageServer : IRename
{
    public RenameRegistrationOptions RenameRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        PrepareProvider = false,
    };

    public Task<WorkspaceEdit?> RenameAsync(RenameParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult<WorkspaceEdit?>(null);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);

        var referencedSymbol = syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => semanticModel.GetReferencedSymbol(symbol) ?? semanticModel.GetDeclaredSymbol(symbol))
            .LastOrDefault(symbol => symbol is not null);
        if (referencedSymbol is null) return Task.FromResult<WorkspaceEdit?>(null);

        // TODO: Check if symbol is owned by this compilation
        // TODO: Go through all nodes of all trees and rewrite

        throw new NotImplementedException();
    }
}
