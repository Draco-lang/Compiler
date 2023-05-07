using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;
using DocumentDiagnosticReport = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RelatedFullDocumentDiagnosticReport, Draco.Lsp.Model.RelatedUnchangedDocumentDiagnosticReport>;

namespace Draco.LanguageServer;

internal partial class DracoLanguageServer : IPullDiagnostics
{
    public DiagnosticRegistrationOptions DiagnosticRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        Identifier = "draco", // TODO: do we need this? does it do anything
        InterFileDependencies = true,
        WorkspaceDiagnostics = true,
    };

    public Task<DocumentDiagnosticReport> DocumentDiagnosticsAsync(DocumentDiagnosticParams param, CancellationToken cancellationToken)
    {
        this.UpdateCompilation(param.TextDocument.Uri);
        var diags = this.semanticModel.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        return Task.FromResult<DocumentDiagnosticReport>(new RelatedFullDocumentDiagnosticReport()
        {
            Items = lspDiags,
            // TODO: related documents for future
        });
    }

    public Task<WorkspaceDiagnosticReport> WorkSpaceDiagnosticsAsync(WorkspaceDiagnosticParams param, CancellationToken cancellationToken)
    {
        var fileDiags = new List<OneOf<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>>();
        foreach (var file in this.compilation.SyntaxTrees)
        {
            var model = this.compilation.GetSemanticModel(file);
            var diags = model.Diagnostics;
            var lspDiags = diags.Select(Translator.ToLsp).ToList();
            var fileDiag = new WorkspaceFullDocumentDiagnosticReport()
            {
                Items = lspDiags,
                Uri = DocumentUri.From(file.SourceText.Path!),
                Version = 1, //TODO: what is this? is this required?
            };
            fileDiags.Add(fileDiag);
        }
        return Task.FromResult(new WorkspaceDiagnosticReport()
        {
            Items = fileDiags,
        });
    }
}
