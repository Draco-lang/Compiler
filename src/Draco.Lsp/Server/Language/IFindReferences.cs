using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

public interface IFindReferences
{
    [Capability(nameof(ServerCapabilities.ReferencesProvider))]
    public ReferenceOptions? Capability => null;

    [RegistrationOptions("textDocument/references")]
    public ReferenceRegistrationOptions FindReferencesRegistrationOptions { get; }

    [Request("textDocument/references")]
    public Task<IList<Location>> FindReferencesAsync(ReferenceParams param, CancellationToken cancellationToken);
}
