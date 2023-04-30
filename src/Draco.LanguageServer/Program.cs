using System;
using System.CommandLine;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Server;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

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

        var runCommand = new Command("run", "Runs the language server")
        {
            stdioFlag,
        };
        runCommand.SetHandler(RunServerAsync, stdioFlag);

        var checkForUpdatesCommand = new Command("check-for-updates", "Checks for language server updates");
        checkForUpdatesCommand.SetHandler(CheckForUpdatesAsync);

        var rootCommand = new RootCommand("Language Server for Draco");
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(checkForUpdatesCommand);

        await rootCommand.InvokeAsync(args);
    }

    internal static async Task RunServerAsync(bool stdioFlag)
    {
        var transportKind = GetTransportKind(stdioFlag);
        var transportStream = BuildTransportStream(transportKind);

        var client = Lsp.Server.LanguageServer.Connect(transportStream);
        var server = new DracoLanguageServer(client);
        await client.RunAsync(server);
    }

    internal static async Task CheckForUpdatesAsync()
    {
        // Retrieve the latest and current version
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();
        var versions = await resource.GetAllVersionsAsync(
            "Draco.LanguageServer",
            cache,
            NullLogger.Instance,
            CancellationToken.None);
        var latest = versions.Max();
        var current = Assembly.GetExecutingAssembly().GetName().Version;

        // If either are null, bail out
        if (latest is null || current is null)
        {
            Environment.Exit(0);
            return;
        }

        // Otherwise, compare
        if (latest.Version > current)
        {
            // We have a newer version, signal with an exit code of 1
            Environment.Exit(1);
            return;
        }

        // Not newer
        Environment.Exit(0);
        return;
    }

    private static TransportKind GetTransportKind(bool stdioFlag) => stdioFlag
        ? TransportKind.Stdio
        : TransportKind.Unknown;

    private static IDuplexPipe BuildTransportStream(TransportKind transportKind)
    {
        if (transportKind == TransportKind.Stdio)
        {
            return new StdioDuplexPipe();
        }

        Console.Error.WriteLine($"The transport kind {transportKind} is not yet supported");
        Environment.Exit(1);
        return null;
    }

    private class StdioDuplexPipe : IDuplexPipe
    {
        public PipeReader Input { get; } = PipeReader.Create(Console.OpenStandardInput());

        public PipeWriter Output { get; } = PipeWriter.Create(Console.OpenStandardOutput());
    }
}
