using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Draco.LanguageServer;

internal sealed class DracoDocumentRepository
{
    private readonly Dictionary<DocumentUri, string> documents = new();

    public void AddOrUpdateDocument(DocumentUri uri, string contents) => this.documents[uri] = contents;

    public string GetDocument(DocumentUri uri)
    {
        if (this.documents.TryGetValue(uri, out var contents))
        {
            return contents;
        }
        return string.Empty;
    }
}
