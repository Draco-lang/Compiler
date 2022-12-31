using System;
using System.Threading.Tasks;
using Draco.LanguageServer.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Draco.LanguageServer;

internal class Program
{
    internal static async Task Main(string[] args)
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .WithHandler<DracoDocumentHandler>()
            .WithHandler<DracoSemanticTokensHandler>()
            .WithHandler<DracoDocumentFormattingHandler>()
            .WithHandler<DracoGoToDefinitionHandler>()
            .WithHandler<DracoHoverHandler>()
            .WithServices(services => services
                .AddSingleton<DracoDocumentRepository>()));
        await server.WaitForExit;
    }
}
