using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Draco.LanguageServer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
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

    private static void LogException(Exception exception)
    {
        server.LogError($"Draco language server failed with following error: {exception.Message}");
    }

    internal static void Try(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }

    internal static T Try<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            LogException(ex);
            throw;
        }
    }
}
