using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
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
        // TODO: Share compilation
        var souceText = this.documentRepository.GetDocument(param.TextDocument.Uri);
        var syntaxTree = SyntaxTree.Parse(souceText);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var referencedSymbol = syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(semanticModel.GetReferencedSymbol)
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
