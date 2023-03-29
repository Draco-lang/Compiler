using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
using Draco.Lsp.Model;
using Draco.Lsp.Serialization;
using Draco.Lsp.Server;
using Draco.Lsp.Server.TextDocument;
using Nerdbank.Streams;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using StreamJsonRpc;

namespace Draco.LanguageServer;

internal sealed class DracoLanguageServer : ILanguageServer, ITextDocument
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

    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param, CancellationToken cancellationToken) => Task.CompletedTask;
    public async Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param, CancellationToken cancellationToken)
    {
        var uri = param.TextDocument.Uri;
        var change = param.ContentChanges.First();
        var sourceText = change.Text;
        this.documentRepository.AddOrUpdateDocument(uri, sourceText);
        await this.PublishDiagnosticsAsync(uri, sourceText);
    }

    private async Task PublishDiagnosticsAsync(DocumentUri uri, string text)
    {
        // TODO: When becomes incrmental, should not re-create
        var sourceText = SourceText.FromText(uri.ToUri(), text.AsMemory());
        var syntaxTree = SyntaxTree.Parse(sourceText);
        // TODO: Compilation should be shared
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        var diags = compilation.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        await this.client.PublishDiagnosticsAsync(new()
        {
            Uri = uri,
            Diagnostics = lspDiags,
        });
    }
}

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var stream = FullDuplexStream.Splice(Console.OpenStandardInput(), Console.OpenStandardOutput());
        var client = Lsp.Server.LanguageServer.Connect(stream);
        var server = new DracoLanguageServer(client);
        await client.RunAsync(server);
    }
}
