using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

public interface IHover
{
    [Capability(nameof(ServerCapabilities.HoverProvider))]
    public HoverOptions? Capability => null;

    [RegistrationOptions("textDocument/hover")]
    public HoverRegistrationOptions HoverRegistrationOptions { get; }

    [Request("textDocument/hover")]
    public Task<Hover?> HoverAsync(HoverParams param, CancellationToken cancellationToken);
}
