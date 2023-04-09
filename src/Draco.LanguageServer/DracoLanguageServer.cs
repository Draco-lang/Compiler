using System.Collections.Generic;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
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
    private readonly DracoConfigurationRepository configurationRepository;
    private readonly DracoDocumentRepository documentRepository = new();
    private Compilation compilation;
    private SemanticModel semanticModel;
    private SyntaxTree syntaxTree;

    public DracoLanguageServer(ILanguageClient client)
    {
        this.client = client;
        this.configurationRepository = new(client);
    }

    public void Dispose() { }

    public async Task InitializedAsync(InitializedParams param)
    {
        await this.configurationRepository.UpdateConfigurationAsync();
    }

    public Task ShutdownAsync() => Task.CompletedTask;
}
