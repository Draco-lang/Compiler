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
        var cursorPosition = Translator.ToCompiler(param.Position);

        var referencedSymbol = this.syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(this.semanticModel.GetReferencedSymbol)
            .LastOrDefault(symbol => symbol is not null);

        if (referencedSymbol is not null && referencedSymbol.Definition is not null)
        {
            var location = Translator.ToLsp(referencedSymbol.Definition);
            return Task.FromResult<IList<Location>>(new[] { location ?? new() });
        }
        else
        {
            return Task.FromResult<IList<Location>>(Array.Empty<Location>());
        }
    }
}
