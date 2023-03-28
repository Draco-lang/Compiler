using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server;

[Capability(nameof(ServerCapabilities.TextDocumentSync))]
public interface ITextDocumentSyncCapability
{
    public TextDocumentSyncOptions? Capability { get; }

    [RegistrationOptions("textDocument/didOpen")]
    public TextDocumentRegistrationOptions DidOpenRegistrationOptions { get; }

    [RegistrationOptions("textDocument/didChange")]
    public TextDocumentChangeRegistrationOptions DidChangeRegistrationOptions { get; }

    [RegistrationOptions("textDocument/didClose")]
    public TextDocumentRegistrationOptions DidCloseRegistrationOptions { get; }

    [Notification("textDocument/didOpen")]
    public Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param);

    [Notification("textDocument/didChange")]
    public Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param);

    [Notification("textDocument/didClose")]
    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param);
}
