using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax.Extensions;
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
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null) return Task.FromResult<IList<Location>>(Array.Empty<Location>());

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var cursorPosition = Translator.ToCompiler(param.Position);

        var referencedSymbol = syntaxTree.Root
            .TraverseAtPosition(cursorPosition)
            .Select(semanticModel.GetReferencedSymbol)
            .LastOrDefault(symbol => symbol is not null);

        if (referencedSymbol is null) return Task.FromResult<IList<Location>>([]);

        if (referencedSymbol.IsGenericInstance)
        {
            // Unwrap to resolve to the generic definition
            referencedSymbol = referencedSymbol.GenericDefinition;
        }

        if (referencedSymbol.Definition is null) return Task.FromResult<IList<Location>>([]);

        var location = Translator.ToLsp(referencedSymbol.Definition);
        return Task.FromResult<IList<Location>>([location ?? default!]);
    }
}
