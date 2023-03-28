using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Serialization;
using Draco.Lsp.Server;
using Nerdbank.Streams;
using Newtonsoft.Json;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using StreamJsonRpc;

namespace Draco.LanguageServer;

internal sealed class DracoLanguageServer : ILanguageServer, ITextDocumentSyncCapability
{
    public InitializeResult.ServerInfoResult? Info => null;

    public TextDocumentSyncOptions? Capability => null;

    public TextDocumentRegistrationOptions DidOpenRegistrationOptions => new()
    {
        DocumentSelector = new List<DocumentFilter>()
        {
            new()
            {
                Language = "draco",
                Pattern = "**/*.draco",
            }
        }
    };

    public TextDocumentChangeRegistrationOptions DidChangeRegistrationOptions => new()
    {
        DocumentSelector = new List<DocumentFilter>()
        {
            new()
            {
                Language = "draco",
                Pattern = "**/*.draco",
            }
        },
        SyncKind = TextDocumentSyncKind.Full,
    };

    public TextDocumentRegistrationOptions DidCloseRegistrationOptions => new()
    {
        DocumentSelector = new List<DocumentFilter>()
        {
            new()
            {
                Language = "draco",
                Pattern = "**/*.draco",
            }
        }
    };

    private readonly ILanguageClient languageClient;

    public DracoLanguageServer(ILanguageClient languageClient)
    {
        this.languageClient = languageClient;
    }

    public void Dispose()
    {
    }

    public async Task InitializedAsync(InitializedParams param)
    {
        await this.languageClient.LogMessageAsync(new()
        {
            Message = "Hello new LSP impl",
            Type = MessageType.Info,
        });
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }

    public Task TextDocumentDidOpenAsync(DidOpenTextDocumentParams param)
    {
        return Task.CompletedTask;
    }

    public Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param)
    {
        return Task.CompletedTask;
    }

    public Task TextDocumentDidCloseAsync(DidCloseTextDocumentParams param)
    {
        return Task.CompletedTask;
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
