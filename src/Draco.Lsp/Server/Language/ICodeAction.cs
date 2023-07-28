using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.Language;

[ClientCapability("TextDocument.CodeAction")]
public interface ICodeAction
{
    [ServerCapability(nameof(ServerCapabilities.CodeActionProvider))]
    public ICodeActionOptions Capability => this.CodeActionRegistrationOptions;

    [RegistrationOptions("textDocument/codeAction")]
    public CodeActionRegistrationOptions CodeActionRegistrationOptions { get; }

    [Request("textDocument/codeAction")]
    public Task<IList<OneOf<Command, CodeAction>>?> CodeActionAsync(CodeActionParams param, CancellationToken cancellationToken);
}
