using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
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
    private readonly DocumentSelector documentSelector = new(new DocumentFilter
    {
        Pattern = $"**/*{Constants.DracoSourceExtension}",
    });

    public DracoDocumentHandler(ILanguageServerFacade server)
    {
        this.server = server;
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
        DracoDocumentRepository.Documents.AddOrUpdateDocument(uri.Path, sourceText);
        await this.PublishSyntaxDiagnosticsAsync(uri, sourceText);
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
        DracoDocumentRepository.Documents.AddOrUpdateDocument(uri.Path, sourceText);
        await this.PublishSyntaxDiagnosticsAsync(uri, sourceText);
        return Unit.Value;
    }

    private Task PublishSyntaxDiagnosticsAsync(DocumentUri uri, string text)
    {
        var parseTree = ParseTree.Parse(text);
        var diags = parseTree.GetAllDiagnostics();
        this.server.TextDocument.PublishDiagnostics(new()
        {
            Uri = uri,
            Diagnostics = diags.Select(Translator.ToLsp).ToList(),
        });
        return Task.CompletedTask;
    }
}
