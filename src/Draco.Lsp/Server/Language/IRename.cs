using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

[ClientCapability("TextDocument.Rename")]
public interface IRename
{
    [ServerCapability(nameof(ServerCapabilities.RenameProvider))]
    public IRenameOptions Capability => this.RenameRegistrationOptions;

    [RegistrationOptions("textDocument/rename")]
    public RenameRegistrationOptions RenameRegistrationOptions { get; }

    [Request("textDocument/rename")]
    public Task<WorkspaceEdit?> RenameAsync(RenameParams param, CancellationToken cancellationToken);
}
