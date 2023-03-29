using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.TextDocument;

public interface ITextDocumentDidOpen
{
    [RegistrationOptions("textDocument/didOpen")]
    public TextDocumentRegistrationOptions? DidOpenRegistrationOptions { get; }

    [Notification("textDocument/didOpen")]
    public Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param, CancellationToken cancellationToken);
}
