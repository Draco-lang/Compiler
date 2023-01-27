using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer.Handlers;

internal sealed class DracoGoToDefinitionHandler : DefinitionHandlerBase
{
    private readonly DracoDocumentRepository documentRepository;

    public DracoGoToDefinitionHandler(DracoDocumentRepository documentRepository)
    {
        this.documentRepository = documentRepository;
    }

    protected override DefinitionRegistrationOptions CreateRegistrationOptions(
        DefinitionCapability capability,
        ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = Constants.DracoSourceDocumentSelector
        };

    public override Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(request.Position);
        // TODO: Share compilation
        var souceText = this.documentRepository.GetDocument(request.TextDocument.Uri);
        var parseTree = SyntaxTree.Parse(souceText);
        var compilation = Compilation.Create(parseTree);
        var semanticModel = compilation.GetSemanticModel();

        var referencedSymbol = parseTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(semanticModel.GetReferencedSymbolOrNull)
            .LastOrDefault(symbol => symbol is not null);

        if (referencedSymbol is not null && referencedSymbol.Definition is not null)
        {
            var location = Translator.ToLsp(referencedSymbol.Definition);
            return Task.FromResult(new LocationOrLocationLinks(location ?? new()));
        }
        else
        {
            return Task.FromResult(new LocationOrLocationLinks());
        }
    }
}
