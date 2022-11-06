using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Draco.LanguageServer;

internal sealed class DracoDocumentRepository
{
    private readonly Dictionary<DocumentUri, string> Documents = new();

    public void AddOrUpdateDocument(DocumentUri uri, string contents)
    {
        this.Documents[uri] = contents;
    }

    public string GetDocument(DocumentUri uri)
    {
        if (this.Documents.TryGetValue(uri, out var contents))
        {
            return contents;
        }
        return string.Empty;
    }
}
