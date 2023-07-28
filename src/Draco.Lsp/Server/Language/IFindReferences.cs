using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

[ClientCapability("TextDocument.References")]
public interface IFindReferences
{
    [ServerCapability(nameof(ServerCapabilities.ReferencesProvider))]
    public IReferenceOptions Capability => this.FindReferencesRegistrationOptions;

    [RegistrationOptions("textDocument/references")]
    public ReferenceRegistrationOptions FindReferencesRegistrationOptions { get; }

    [Request("textDocument/references")]
    public Task<IList<Location>> FindReferencesAsync(ReferenceParams param, CancellationToken cancellationToken);
}
