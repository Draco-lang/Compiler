using System.Collections.Generic;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server;

namespace Draco.LanguageServer;

internal sealed partial class DracoLanguageServer : ILanguageServer
{
    public InitializeResult.ServerInfoResult Info => new()
    {
        Name = "Draco Language Server",
        Version = "0.1.0",
    };

    public IList<DocumentFilter> DocumentSelector => new[]
    {
        new DocumentFilter()
        {
            Language = "draco",
            Pattern = "**/*.draco",
        }
    };
    public TextDocumentSyncKind SyncKind => TextDocumentSyncKind.Full;

    private readonly ILanguageClient client;
    private readonly DracoDocumentRepository documentRepository = new();

    public DracoLanguageServer(ILanguageClient client)
    {
        this.client = client;
    }

    public void Dispose() { }

    public Task InitializedAsync(InitializedParams param) => Task.CompletedTask;
    public Task ShutdownAsync() => Task.CompletedTask;
}
