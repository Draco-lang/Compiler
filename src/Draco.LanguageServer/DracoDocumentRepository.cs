using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Draco.LanguageServer;

internal class DracoDocumentRepository
{
    public Dictionary<DocumentUri, string> Documents { get; } = new();
    public void AddOrUpdate(DocumentUri uri, string contents)
    {
        if (this.Documents.TryGetValue(uri, out var document))
        {
            this.Documents[uri] = contents;
        }
        else
        {
            this.Documents.Add(uri, contents);
        }
    }
}
