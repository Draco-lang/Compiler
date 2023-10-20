using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.TextDocument;

[ClientCapability("TextDocument.Synchronization")]
public interface ITextDocumentDidOpen
{
    [RegistrationOptions("textDocument/didOpen")]
    public TextDocumentRegistrationOptions DidOpenRegistrationOptions { get; }

    [Notification("textDocument/didOpen", Mutating = true)]
    public Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param);
}
