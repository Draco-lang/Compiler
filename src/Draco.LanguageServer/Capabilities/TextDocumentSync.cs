using System;
using System.Collections.Immutable;
using System.IO;
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
        this.UpdateCompilation(param.TextDocument.Text);
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
        this.UpdateCompilation(sourceText);
        await this.PublishDiagnosticsAsync(uri, sourceText);
    }

    // NOTE: This needs to be more sophisticated, once we have multiple files and such
    private void UpdateCompilation(string text)
    {
        this.syntaxTree = SyntaxTree.Parse(text);
        this.compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(this.syntaxTree),
            // NOTE: Temporary until we solve MSBuild communication
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        this.semanticModel = this.compilation.GetSemanticModel(this.syntaxTree);
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
