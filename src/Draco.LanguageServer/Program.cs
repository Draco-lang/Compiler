using System;
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

internal sealed class DracoLanguageServer : ILanguageServer
{
    private readonly ILanguageClient languageClient;

    public DracoLanguageServer(ILanguageClient languageClient)
    {
        this.languageClient = languageClient;
    }

    public void Dispose()
    {
    }

    public async Task<InitializeResult> InitializeAsync(InitializeParams param)
    {
        return new InitializeResult()
        {
            Capabilities = new ServerCapabilities()
            {
            },
        };
    }

    public async Task InitializedAsync(InitializedParams param)
    {
        await this.languageClient.LogMessageAsync(new LogMessageParams()
        {
            Type = MessageType.Error,
            Message = "SEND HELP",
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
