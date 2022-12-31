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

internal class DracoHoverHandler : HoverHandlerBase
{
    private readonly DracoDocumentRepository documentRepository;

    public DracoHoverHandler(DracoDocumentRepository documentRepository)
    {
        this.documentRepository = documentRepository;
    }

    public override async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var cursorPosition = Translator.ToCompiler(request.Position);
        // TODO: Share compilation
        var souceText = this.documentRepository.GetDocument(request.TextDocument.Uri);
        var parseTree = ParseTree.Parse(souceText);
        var compilation = Compilation.Create(parseTree);
        var semanticModel = compilation.GetSemanticModel();

        var referencedSymbol = parseTree
            .TraverseSubtreesAtPosition(cursorPosition)
            .Select(semanticModel.GetReferencedSymbolOrNull)
            .LastOrDefault(symbol => symbol is not null);

        if (referencedSymbol is null)
        {
            referencedSymbol = parseTree
                .TraverseSubtreesAtPosition(cursorPosition)
                .Select(semanticModel.GetDefinedSymbolOrNull)
                .LastOrDefault(symbol => symbol is not null);
        }

        if (referencedSymbol is IVariableSymbol symb) return new Hover()
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
            {
                Kind = MarkupKind.Markdown,
                Value = symb.Documentation
            })
        };
        else if (referencedSymbol is IFunctionSymbol smb) return new Hover()
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
            {
                Kind = MarkupKind.Markdown,
                Value = smb.Documentation
            })
        };
        else return new Hover()
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
            {
                Kind = MarkupKind.PlainText,
                Value = ""
            })
        };
    }

    protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = new DocumentSelector(new DocumentFilter
        {
            Pattern = $"**/*{Constants.DracoSourceExtension}",
        })
    };
}
