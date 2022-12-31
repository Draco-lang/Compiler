using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer.Handlers;
internal sealed class DracoFindAllReferencesHandler : ReferencesHandlerBase
{
    private readonly DracoDocumentRepository documentRepository;

    public DracoFindAllReferencesHandler(DracoDocumentRepository documentRepository)
    {
        this.documentRepository = documentRepository;
    }

    protected override ReferenceRegistrationOptions CreateRegistrationOptions(
        ReferenceCapability capability,
        ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = new DocumentSelector(new DocumentFilter
            {
                Pattern = $"**/*{Constants.DracoSourceExtension}",
            })
        };

    public override Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        // TODO: Share compilation
        var cursorPosition = Translator.ToCompiler(request.Position);
        var souceText = this.documentRepository.GetDocument(request.TextDocument.Uri);
        var parseTree = ParseTree.Parse(souceText);
        var compilation = Compilation.Create(parseTree);
        var semanticModel = compilation.GetSemanticModel();

        var referencedSymbol = parseTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => semanticModel.GetReferencedSymbolOrNull(symbol) ?? semanticModel.GetDefinedSymbolOrNull(symbol))
            .LastOrDefault(symbol => symbol is not null);

        var nodes = parseTree.Root.InOrderTraverse();
        var references = new List<Location>();

        if (referencedSymbol is not null)
        {
            foreach (var node in nodes)
            {
                if (referencedSymbol.Equals(semanticModel.GetReferencedSymbolOrNull(node)))
                {
                    var location = Translator.ToLsp(node.Location);
                    if (location is not null)
                    {
                        references.Add(location);
                    }
                }
                if (request.Context.IncludeDeclaration && referencedSymbol.Equals(semanticModel.GetDefinedSymbolOrNull(node)))
                {
                    var location = Translator.ToLsp(node.Location);
                    if (location is not null)
                    {
                        references.Add(location);
                    }
                }
            }
        }

        return Task.FromResult(new LocationContainer(references));
    }
}
