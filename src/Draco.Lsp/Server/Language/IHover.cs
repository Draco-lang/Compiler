using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

[ClientCapability("TextDocument.Hover")]
public interface IHover
{
    [ServerCapability(nameof(ServerCapabilities.HoverProvider))]
    public IHoverOptions Capability => this.HoverRegistrationOptions;

    [RegistrationOptions("textDocument/hover")]
    public HoverRegistrationOptions HoverRegistrationOptions { get; }

    [Request("textDocument/hover")]
    public Task<Hover?> HoverAsync(HoverParams param, CancellationToken cancellationToken);
}
