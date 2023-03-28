using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.TextDocument;

public interface ITextDocumentDidChange
{
    [RegistrationOptions("textDocument/didChange")]
    public TextDocumentChangeRegistrationOptions? DidChangeRegistrationOptions { get; }

    [Notification("textDocument/didChange")]
    public Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param);
}
