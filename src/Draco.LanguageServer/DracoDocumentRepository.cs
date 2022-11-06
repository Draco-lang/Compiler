using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Draco.LanguageServer;

internal sealed class DracoDocumentRepository
{
    private Dictionary<DocumentUri, string> Documents { get; } = new();
    public void AddOrUpdateDocument(DocumentUri uri, string contents)
    {
        this.Documents[uri] = contents;
    }
    public string GetDocument(DocumentUri uri)
    {
        if (this.Documents.ContainsKey(uri))
        {
            return this.Documents[uri];
        }
        return "";
    }

}
