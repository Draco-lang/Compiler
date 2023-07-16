using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using DocumentDiagnosticReport = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RelatedFullDocumentDiagnosticReport, Draco.Lsp.Model.RelatedUnchangedDocumentDiagnosticReport>;

namespace Draco.Lsp.Server.Language;

[ClientCapability("TextDocument.Diagnostic")]
public interface IPullDiagnostics
{
    [ServerCapability(nameof(ServerCapabilities.DiagnosticProvider))]
    public IDiagnosticOptions Capability => this.DiagnosticRegistrationOptions;

    [RegistrationOptions("textDocument/diagnostic")]
    public DiagnosticRegistrationOptions DiagnosticRegistrationOptions { get; }

    [Request("textDocument/diagnostic")]
    public Task<DocumentDiagnosticReport> DocumentDiagnosticsAsync(DocumentDiagnosticParams param, CancellationToken cancellationToken);

    [Request("workspace/diagnostic")]
    public Task<WorkspaceDiagnosticReport> WorkspaceDiagnosticsAsync(WorkspaceDiagnosticParams param, CancellationToken cancellationToken);
}
