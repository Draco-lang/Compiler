using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server.TextDocument;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ITextDocumentSync
{
    public async Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param, CancellationToken cancellationToken)
    {
        this.documentRepository.AddOrUpdateDocument(param.TextDocument.Uri, param.TextDocument.Text);
        this.syntaxTree = SyntaxTree.Parse(param.TextDocument.Text);
        this.compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(this.syntaxTree));
        this.semanticModel = this.compilation.GetSemanticModel(this.syntaxTree);
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
        this.syntaxTree = SyntaxTree.Parse(sourceText);
        this.compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(this.syntaxTree));
        this.semanticModel = this.compilation.GetSemanticModel(this.syntaxTree);
        await this.PublishDiagnosticsAsync(uri, sourceText);
    }

    private async Task PublishDiagnosticsAsync(DocumentUri uri, string text)
    {
        var diags = this.compilation.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        await this.client.PublishDiagnosticsAsync(new()
        {
            Uri = uri,
            Diagnostics = lspDiags,
        });
    }
}
