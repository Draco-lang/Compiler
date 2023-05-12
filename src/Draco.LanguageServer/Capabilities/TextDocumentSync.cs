using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server.TextDocument;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ITextDocumentSync
{
    public async Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param, CancellationToken cancellationToken)
    {
        this.documentRepository.AddOrUpdateDocument(param.TextDocument.Uri, param.TextDocument.Text);
        this.UpdateDocument(param.TextDocument.Uri);
        await this.PublishDiagnosticsAsync(param.TextDocument.Uri);
    }

    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param, CancellationToken cancellationToken)
    {
        var uri = param.TextDocument.Uri;
        var change = param.ContentChanges.First();
        var sourceText = change.Text;
        this.documentRepository.AddOrUpdateDocument(uri, sourceText);
        this.UpdateDocument(uri);
        await this.PublishDiagnosticsAsync(uri);
    }

    private void UpdateDocument(DocumentUri documentUri)
    {
        var uri = documentUri.ToUri();
        var oldTree = this.compilation.SyntaxTrees
            .FirstOrDefault(tree => tree.SourceText.Path == uri);
        var newSourceText = this.documentRepository.GetOrCreateDocument(documentUri);
        this.syntaxTree = SyntaxTree.Parse(newSourceText);
        this.compilation = this.compilation.UpdateSyntaxTree(oldTree, this.syntaxTree);
        this.semanticModel = this.compilation.GetSemanticModel(this.syntaxTree);
    }

    private async Task PublishDiagnosticsAsync(DocumentUri uri)
    {
        var diags = this.semanticModel.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        await this.client.PublishDiagnosticsAsync(new()
        {
            Uri = uri,
            Diagnostics = lspDiags,
        });
    }
}
