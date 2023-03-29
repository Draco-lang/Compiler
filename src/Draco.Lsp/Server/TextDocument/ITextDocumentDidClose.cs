using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.TextDocument;

public interface ITextDocumentDidClose
{
    [RegistrationOptions("textDocument/didClose")]
    public TextDocumentRegistrationOptions? DidCloseRegistrationOptions { get; }

    [Notification("textDocument/didClose")]
    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param, CancellationToken cancellationToken);
}
