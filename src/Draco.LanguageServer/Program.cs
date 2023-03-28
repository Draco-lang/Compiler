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

namespace Draco.LanguageServer;

internal sealed class LanguageServer : ILanguageServer
{
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
            await server.RunAsync(stream);
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(@"C:\TMP\lsp_err_log.txt", $"{ex.Message}\n{ex.StackTrace}");
        }
    }
}
