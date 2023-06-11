using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using DocumentDiagnosticReport = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RelatedFullDocumentDiagnosticReport, Draco.Lsp.Model.RelatedUnchangedDocumentDiagnosticReport>;

namespace Draco.Lsp.Server.Language;

public interface IPullDiagnostics
{
    [Capability(nameof(ServerCapabilities.CompletionProvider))]
    public DiagnosticOptions? Capability => null;

    [RegistrationOptions("textDocument/diagnostic")]
    public DiagnosticRegistrationOptions DiagnosticRegistrationOptions { get; }

    [Request("textDocument/diagnostic")]
    public Task<DocumentDiagnosticReport> DocumentDiagnosticsAsync(DocumentDiagnosticParams param, CancellationToken cancellationToken);

    [Request("workspace/diagnostic")]
    public Task<WorkspaceDiagnosticReport> WorkSpaceDiagnosticsAsync(WorkspaceDiagnosticParams param, CancellationToken cancellationToken);
}
