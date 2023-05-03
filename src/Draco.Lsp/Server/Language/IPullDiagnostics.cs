using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Draco.Lsp.Server.Language;

public interface IPullDiagnostics
{
    [Capability(nameof(ServerCapabilities.CompletionProvider))]
    public DiagnosticOptions? Capability => null;

    [RegistrationOptions("textDocument/diagnostic")]
    public DiagnosticRegistrationOptions DiagnosticRegistrationOptions { get; }

    [Request("textDocument/diagnostic")]
    public Task<DocumentDiagnosticReport> DocumentDiagnosticsAsync(DocumentDiagnosticParams param, CancellationToken cancellationToken);
}
