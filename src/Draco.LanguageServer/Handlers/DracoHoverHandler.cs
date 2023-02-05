using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer.Handlers;

internal sealed class DracoHoverHandler : HoverHandlerBase
{
    private readonly DracoDocumentRepository documentRepository;

    public DracoHoverHandler(DracoDocumentRepository documentRepository)
    {
        this.documentRepository = documentRepository;
    }

    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = Constants.DracoSourceDocumentSelector
    };

    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(request.Position);
        // TODO: Share compilation
        var souceText = this.documentRepository.GetDocument(request.TextDocument.Uri);
        var syntaxTree = SyntaxTree.Parse(souceText);
        var compilation = Compilation.Create(syntaxTree);
        var semanticModel = compilation.GetSemanticModel();

        var referencedSymbol = syntaxTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(symbol => semanticModel.GetReferencedSymbolOrNull(symbol) ?? semanticModel.GetDefinedSymbolOrNull(symbol))
            .LastOrDefault(symbol => symbol is not null);

        var docs = referencedSymbol is null ? string.Empty : referencedSymbol.Documentation;

        return Task.FromResult<Hover?>(new Hover()
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
            {
                Kind = MarkupKind.Markdown,
                Value = docs
            })
        });
    }
}
