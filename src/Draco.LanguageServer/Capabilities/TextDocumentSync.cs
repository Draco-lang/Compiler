using System;
using System.Linq;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.TextDocument;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ITextDocumentSync
{
    public async Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param)
    {
        await this.UpdateDocument(param.TextDocument.Uri, param.TextDocument.Text);
    }

    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param) =>
        Task.CompletedTask;

    public async Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param)
    {
        var uri = param.TextDocument.Uri;
        var change = param.ContentChanges.First();
        var sourceText = change.Text;
        await this.UpdateDocument(uri, sourceText);
    }

    private async Task PublishDiagnosticsAsync(DocumentUri uri)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, uri);

        if (syntaxTree is null)
        {
            await this.client.PublishDiagnosticsAsync(new()
            {
                Uri = uri,
                Diagnostics = Array.Empty<Diagnostic>(),
            });
            return;
        }

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var diags = semanticModel.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();

        await this.client.PublishDiagnosticsAsync(new()
        {
            Uri = uri,
            Diagnostics = lspDiags,
        });
    }
}
