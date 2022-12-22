using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Draco.LanguageServer.Handlers;

/// <summary>
/// Handles text document changes in draco source files.
/// </summary>
internal sealed class DracoDocumentHandler : TextDocumentSyncHandlerBase
{
    // TODO: Change this to diffs once we have incremental parsing
    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    private readonly ILanguageServerFacade server;
    private readonly DracoDocumentRepository repository;
    private readonly DocumentSelector documentSelector = new(new DocumentFilter
    {
        Pattern = $"**/*{Constants.DracoSourceExtension}",
    });

    public DracoDocumentHandler(ILanguageServerFacade server, DracoDocumentRepository repository)
    {
        this.server = server;
        this.repository = repository;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) =>
        new(uri, Constants.LanguageId);

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        SynchronizationCapability capability,
        ClientCapabilities clientCapabilities) => new()
        {
            DocumentSelector = this.documentSelector,
            Change = this.Change,
            Save = new SaveOptions()
            {
                IncludeText = true,
            },
        };

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;
        var sourceText = request.TextDocument.Text;
        this.repository.AddOrUpdateDocument(uri, sourceText);
        await this.PublishDiagnosticsAsync(uri, sourceText);
        return Unit.Value;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken) =>
        Unit.Task;

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken) =>
        Unit.Task;

    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var uri = request.TextDocument.Uri;
        var change = request.ContentChanges.First();
        var sourceText = change.Text;
        this.repository.AddOrUpdateDocument(uri, sourceText);
        await this.PublishDiagnosticsAsync(uri, sourceText);
        return Unit.Value;
    }

    private Task PublishDiagnosticsAsync(DocumentUri uri, string text)
    {
        var parseTree = ParseTree.Parse(text);
        // TODO: Compilation should be shared
        var compilation = Compilation.Create(parseTree);
        var diags = compilation.GetDiagnostics();
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        this.server.TextDocument.PublishDiagnostics(new()
        {
            Uri = uri,
            Diagnostics = lspDiags,
        });
        return Task.CompletedTask;
    }
}
