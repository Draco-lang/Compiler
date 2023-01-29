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
    private readonly DocumentSelector documentSelector = Constants.DracoSourceDocumentSelector;

    public DracoDocumentFormattingHandler(DracoDocumentRepository repository)
    {
        this.repository = repository;
    }

    public override Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri.ToUri();
        var sourceText = this.repository.GetDocument(request.TextDocument.Uri);
        var tree = Program.Try(() => ParseTree.Parse(sourceText));
        var originalRange = tree.Root.Range;
        tree = Program.Try(() => tree.Format());
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
