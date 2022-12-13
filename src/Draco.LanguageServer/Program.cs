using System;
using System.Threading.Tasks;
using Draco.LanguageServer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace Draco.LanguageServer;

internal static class Program
{
    private static OmniSharp.Extensions.LanguageServer.Server.LanguageServer server;
    internal static async Task Main(string[] args)
    {
        server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .WithHandler<DracoDocumentHandler>()
            .WithHandler<DracoSemanticTokensHandler>()
            .WithHandler<DracoDocumentFormattingHandler>()
            .WithServices(services => services
                .AddSingleton<DracoDocumentRepository>()));
        await server.WaitForExit;
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
