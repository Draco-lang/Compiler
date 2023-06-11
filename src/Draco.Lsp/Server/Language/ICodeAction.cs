using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

public interface ICodeAction
{
    [Capability(nameof(ServerCapabilities.CompletionProvider))]
    public CodeActionOptions? Capability => null;

    [RegistrationOptions("textDocument/codeAction")]
    public CodeActionRegistrationOptions CodeActionRegistrationOptions { get; }

    [Request("textDocument/codeAction")]
    public Task<IList<OneOf<Command, CodeAction>>?> CodeActionAsync(CodeActionParams param, CancellationToken cancellationToken);
}
