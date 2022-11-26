using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Draco.LanguageServer.Handlers;

internal sealed class DracoDocumentFormattingHandler : DocumentFormattingHandlerBase
{
    private readonly DracoDocumentRepository repository;
    private readonly DocumentSelector documentSelector = new(new DocumentFilter
    {
        Pattern = $"**/*{Constants.DracoSourceExtension}",
    });

    public DracoDocumentFormattingHandler(DracoDocumentRepository repository)
    {
        this.repository = repository;
    }

    public override Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
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
        var container = new TextEditContainer(edit);
        return Task.FromResult(container)!;
    }

    protected override DocumentFormattingRegistrationOptions CreateRegistrationOptions(DocumentFormattingCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = this.documentSelector
        };
}
