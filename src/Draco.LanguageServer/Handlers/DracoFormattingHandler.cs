using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer.Handlers;

internal sealed class DracoFormattingHandler : DocumentFormattingHandlerBase
{
    private readonly DracoDocumentRepository repository;
    private readonly DocumentSelector documentSelector = new(new DocumentFilter
    {
        Pattern = $"**/*{Constants.DracoSourceExtension}",
    });

    public DracoFormattingHandler(DracoDocumentRepository repository)
    {
        this.repository = repository;
    }

    public override async Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        var source = this.repository.GetDocument(request.TextDocument.Uri);
        var tree = ParseTree.Parse(source);
        var originalRange = tree.Range;
        tree = tree.Format();
        var edit = new TextEdit()
        {
            NewText = tree.ToString(),
            Range = Translator.ToLsp(originalRange),
        };
        return new TextEditContainer(edit);
    }

    protected override DocumentFormattingRegistrationOptions CreateRegistrationOptions(DocumentFormattingCapability capability, ClientCapabilities clientCapabilities) =>
        new DocumentFormattingRegistrationOptions()
        {
            DocumentSelector = this.documentSelector
        };
}
