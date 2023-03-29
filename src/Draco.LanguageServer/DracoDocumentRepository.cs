using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Lsp.Model;

namespace Draco.LanguageServer;

internal sealed class DracoDocumentRepository
{
    private readonly Dictionary<DocumentUri, SourceText> documents = new();

    public void AddOrUpdateDocument(DocumentUri uri, string contents) =>
        this.documents[uri] = SourceText.FromText(uri.ToUri(), contents.AsMemory());

    public SourceText GetDocument(DocumentUri uri)
    {
        if (this.documents.TryGetValue(uri, out var contents))
        {
            return contents;
        }
        return SourceText.None;
    }
}
