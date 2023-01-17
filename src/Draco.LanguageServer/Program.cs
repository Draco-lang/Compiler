using System;
using System.CommandLine;
using System.Threading.Tasks;
using Draco.LanguageServer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Draco.LanguageServer;

internal enum TransportKind
{
    Unknown = 0,
    Stdio = 1,
    Ipc = 2,
    Pipe = 3,
    Socket = 4,
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
        : TransportKind.Unknown;

    private static LanguageServerOptions ConfigureTransportKind(this LanguageServerOptions options, TransportKind transportKind)
    {
        if (transportKind == TransportKind.Stdio)
        {
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput());
            return options;
        }

        Console.Error.WriteLine($"The transport kind {transportKind} is not yet supported");
        Environment.Exit(1);
        return options;
    }
}
