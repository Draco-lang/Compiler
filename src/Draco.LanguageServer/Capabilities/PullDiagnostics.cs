using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;
using DocumentDiagnosticReport = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RelatedFullDocumentDiagnosticReport, Draco.Lsp.Model.RelatedUnchangedDocumentDiagnosticReport>;

namespace Draco.LanguageServer;

internal partial class DracoLanguageServer : IPullDiagnostics
{
    public DiagnosticRegistrationOptions DiagnosticRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        Identifier = "draco", // TODO: do we need this? does it do anything?
        InterFileDependencies = true,
        WorkspaceDiagnostics = true,
    };

    private int diagVersion = 0;

    public async Task<DocumentDiagnosticReport> DocumentDiagnosticsAsync(DocumentDiagnosticParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var syntaxTree = GetSyntaxTree(compilation, param.TextDocument.Uri);
        if (syntaxTree is null)
        {
            return new RelatedFullDocumentDiagnosticReport()
            {
                Items = Array.Empty<Diagnostic>(),
            };
        }

        // Clear push diagnostics to avoid duplicates
        await this.client.PublishDiagnosticsAsync(new()
        {
            Diagnostics = new List<Diagnostic>(),
            Uri = param.TextDocument.Uri
        });

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var diags = semanticModel.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        return new RelatedFullDocumentDiagnosticReport()
        {
            Items = lspDiags,
            // TODO: related documents for future
        };
    }

    public Task<WorkspaceDiagnosticReport> WorkspaceDiagnosticsAsync(WorkspaceDiagnosticParams param, CancellationToken cancellationToken)
    {
        var compilation = this.compilation;

        var fileDiags = new List<OneOf<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>>();
        foreach (var file in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(file);
            var diags = model.Diagnostics;
            var fileDiag = new WorkspaceFullDocumentDiagnosticReport()
            {
                Items = diags.Select(Translator.ToLsp).ToList(),
                Uri = DocumentUri.From(file.SourceText.Path!),
                // TODO: what is this? is this required?
                Version = Interlocked.Increment(ref this.diagVersion),
            };
            fileDiags.Add(fileDiag);
        }
        return Task.FromResult(new WorkspaceDiagnosticReport()
        {
            Items = fileDiags,
        });
    }
}
