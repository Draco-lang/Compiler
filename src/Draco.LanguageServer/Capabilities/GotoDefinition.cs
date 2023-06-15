using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : IGotoDefinition
{
    public DefinitionRegistrationOptions GotoDefinitionRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
    };

    public Task<IList<Location>> GotoDefinitionAsync(DefinitionParams param, CancellationToken cancellationToken)
    {
        var syntaxTree = this.GetSyntaxTree(param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult<IList<Location>>(Array.Empty<Location>());

        var semanticModel = this.compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);

        var referencedSymbol = syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(semanticModel.GetReferencedSymbol)
            .LastOrDefault(symbol => symbol is not null);

        if (referencedSymbol is not null && referencedSymbol.Definition is not null)
        {
            var location = Translator.ToLsp(referencedSymbol.Definition);
            return Task.FromResult<IList<Location>>(new[] { location ?? default! });
        }
        else
        {
            return Task.FromResult<IList<Location>>(Array.Empty<Location>());
        }
    }
}
