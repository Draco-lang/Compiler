using System.Collections.Generic;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server.TextDocument;

public interface ITextDocumentSync : ITextDocumentDidOpen, ITextDocumentDidClose, ITextDocumentDidChange
{
    [Capability(nameof(ServerCapabilities.TextDocumentSync))]
    public TextDocumentSyncOptions Capability => new()
    {
        Change = this.SyncKind,
        OpenClose = true,
        Save = true,
        WillSave = true,
        WillSaveWaitUntil = true,
    };

    public IList<DocumentFilter>? DocumentSelector { get; }
    public TextDocumentSyncKind SyncKind { get; }

    TextDocumentRegistrationOptions ITextDocumentDidOpen.DidOpenRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
    };
    TextDocumentRegistrationOptions ITextDocumentDidClose.DidCloseRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
    };
    TextDocumentChangeRegistrationOptions ITextDocumentDidChange.DidChangeRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        SyncKind = this.SyncKind,
    };
}
