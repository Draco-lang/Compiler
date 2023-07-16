using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

public interface IGotoDefinition
{
    [Capability(nameof(ServerCapabilities.DefinitionProvider))]
    public IDefinitionOptions Capability => this.GotoDefinitionRegistrationOptions;

    [RegistrationOptions("textDocument/definition")]
    public DefinitionRegistrationOptions GotoDefinitionRegistrationOptions { get; }

    [Request("textDocument/definition")]
    public Task<IList<Location>> GotoDefinitionAsync(DefinitionParams param, CancellationToken cancellationToken);
}
