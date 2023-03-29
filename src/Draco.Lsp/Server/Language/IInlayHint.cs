using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

public interface IInlayHint
{
    [Capability(nameof(ServerCapabilities.InlayHintProvider))]
    public InlayHintOptions? Capability => null;

    [RegistrationOptions("textDocument/inlayHint")]
    public InlayHintRegistrationOptions InlayHintRegistrationOptions { get; }

    [Request("textDocument/inlayHint")]
    public Task<IList<InlayHint>> InlayHintAsync(InlayHintParams param, CancellationToken cancellationToken);
}
