using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.TextDocument;

[ClientCapability("TextDocument.Synchronization")]
public interface ITextDocumentDidChange
{
    [RegistrationOptions("textDocument/didChange")]
    public TextDocumentChangeRegistrationOptions DidChangeRegistrationOptions { get; }

    [Notification("textDocument/didChange", Mutating = true)]
    public Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param);
}
