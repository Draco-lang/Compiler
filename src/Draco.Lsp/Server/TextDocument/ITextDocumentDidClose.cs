using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.TextDocument;

[ClientCapability("TextDocument.Synchronization")]
public interface ITextDocumentDidClose
{
    [RegistrationOptions("textDocument/didClose")]
    public TextDocumentRegistrationOptions DidCloseRegistrationOptions { get; }

    [Notification("textDocument/didClose", Mutating = true)]
    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param);
}
