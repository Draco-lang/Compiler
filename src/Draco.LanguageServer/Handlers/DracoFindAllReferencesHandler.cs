using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;
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
            DocumentSelector = Constants.DracoSourceDocumentSelector
        };

    public override Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
    {
        // TODO: Share compilation
        var cursorPosition = Translator.ToCompiler(request.Position);
        var souceText = this.documentRepository.GetDocument(request.TextDocument.Uri);
        var parseTree = SyntaxTree.Parse(souceText);
        var compilation = Compilation.Create(parseTree);
        var semanticModel = compilation.GetSemanticModel();

        var referencedSymbol = parseTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => semanticModel.GetReferencedSymbolOrNull(symbol) ?? semanticModel.GetDefinedSymbolOrNull(symbol))
            .LastOrDefault(symbol => symbol is not null);

        var references = new List<Location>();

        if (referencedSymbol is not null)
        {
            var referencingNodes = FindAllReferences(
                tree: parseTree,
                semanticModel: semanticModel,
                symbol: referencedSymbol,
                includeDeclaration: request.Context.IncludeDeclaration,
                cancellationToken: cancellationToken);
            foreach (var node in referencingNodes)
            {
                var location = Translator.ToLsp(node.Location);
                if (location is not null) references.Add(location);
            }
        }

        return Task.FromResult(new LocationContainer(references));
    }

    private static IEnumerable<SyntaxNode> FindAllReferences(
        SyntaxTree tree,
        SemanticModel semanticModel,
        ISymbol symbol,
        bool includeDeclaration,
        CancellationToken cancellationToken)
    {
        foreach (var node in tree.Root.PreOrderTraverse())
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            if (symbol.Equals(semanticModel.GetReferencedSymbolOrNull(node)))
            {
                yield return node;
            }
            if (includeDeclaration && symbol.Equals(semanticModel.GetDefinedSymbolOrNull(node)))
            {
                yield return node;
            }
        }
    }
}
