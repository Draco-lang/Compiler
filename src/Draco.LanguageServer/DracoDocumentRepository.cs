using System;
using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;

namespace Draco.LanguageServer;

internal sealed class DracoDocumentRepository
{
    private readonly Dictionary<DocumentUri, SourceText> documents = new();

    public SourceText AddOrUpdateDocument(DocumentUri uri, string contents)
    {
        var sourceText = SourceText.FromText(uri.ToUri(), contents.AsMemory());
        this.documents[uri] = sourceText;
        return sourceText;
    }

    public SourceText GetDocument(DocumentUri uri) => this.documents.TryGetValue(uri, out var contents)
        ? contents
        : SourceText.None;
}
