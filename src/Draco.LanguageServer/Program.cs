using System;
using System.CommandLine;
using System.Threading.Tasks;
using Draco.LanguageServer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Draco.LanguageServer;

internal enum TransportKind
{
    Stdio = 0,
    Ipc = 1,
    Pipe = 2,
    Socket = 3,
}

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var stdioFlag = new Option<bool>(name: "--stdio", description: "A flag to set the transportation option to stdio");
        var rootCommand = new RootCommand("Language Server for Draco")
        {
            stdioFlag,
        };
        rootCommand.SetHandler(RunServerAsync, stdioFlag);

        await rootCommand.InvokeAsync(args);
    }

    internal static async Task RunServerAsync(bool stdioFlag)
    {
        var transportKind = GetTransportKind(stdioFlag);

        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
            .ConfigureTransportKind(transportKind)
            .WithHandler<DracoDocumentHandler>()
            .WithHandler<DracoSemanticTokensHandler>()
            .WithHandler<DracoDocumentFormattingHandler>()
            .WithHandler<DracoGoToDefinitionHandler>()
            .WithHandler<DracoFindAllReferencesHandler>()
            .WithHandler<DracoHoverHandler>()
            .WithServices(services => services
                .AddSingleton<DracoDocumentRepository>()));

        await server.WaitForExit;
    }

    private static TransportKind GetTransportKind(bool stdioFlag) => stdioFlag
        ? TransportKind.Stdio
        : throw new NotImplementedException();

    private static LanguageServerOptions ConfigureTransportKind(this LanguageServerOptions options, TransportKind transportKind)
    {
        if (transportKind == TransportKind.Stdio)
        {
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput());
            return options;
        }

        throw new NotSupportedException($"The transport kind {transportKind} is not yet supported");
    }
}
