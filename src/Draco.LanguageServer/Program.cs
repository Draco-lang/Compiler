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

internal sealed class LanguageServer : ILanguageServer
{
    public ILanguageClient languageClient;
    public JsonRpc rpc;

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
        try
        {
            var server = new LanguageServer();
            var stream = FullDuplexStream.Splice(Console.OpenStandardInput(), Console.OpenStandardOutput());
            var (rpc, client) = server.Create(stream);
            server.languageClient = client;
            server.rpc = rpc;
            rpc.StartListening();
            await rpc.Completion;
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(@"C:\TMP\lsp_err_log.txt", $"{ex.Message}\n{ex.StackTrace}");
        }
    }
}
