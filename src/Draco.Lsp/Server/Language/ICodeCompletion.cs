using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

public interface ICodeCompletion
{
    [Capability(nameof(ServerCapabilities.CompletionProvider))]
    public CompletionOptions? Capability => null;

    [RegistrationOptions("textDocument/completion")]
    public CompletionRegistrationOptions CompletionRegistrationOptions { get; }

    [Request("textDocument/completion")]
    public Task<IList<CompletionItem>> CompleteAsync(CompletionParams param, CancellationToken cancellationToken);
}
