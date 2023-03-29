using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
using Draco.Lsp.Model;
using Draco.Lsp.Server.TextDocument;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ITextDocumentSync
{
    public async Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param, CancellationToken cancellationToken)
    {
        this.documentRepository.AddOrUpdateDocument(param.TextDocument.Uri, param.TextDocument.Text);
        await this.PublishDiagnosticsAsync(param.TextDocument.Uri, param.TextDocument.Text);
    }

    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param, CancellationToken cancellationToken)
    {
        var uri = param.TextDocument.Uri;
        var change = param.ContentChanges.First();
        var sourceText = change.Text;
        this.documentRepository.AddOrUpdateDocument(uri, sourceText);
        await this.PublishDiagnosticsAsync(uri, sourceText);
    }

    private async Task PublishDiagnosticsAsync(DocumentUri uri, string text)
    {
        // TODO: When becomes incrmental, should not re-create
        var sourceText = SourceText.FromText(uri.ToUri(), text.AsMemory());
        var syntaxTree = SyntaxTree.Parse(sourceText);
        // TODO: Compilation should be shared
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        var diags = compilation.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        await this.client.PublishDiagnosticsAsync(new()
        {
            Uri = uri,
            Diagnostics = lspDiags,
        });
    }
}
