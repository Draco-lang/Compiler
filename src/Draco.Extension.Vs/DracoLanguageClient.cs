using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Draco.Extension.Vs
{
    [ContentType("draco")]
    [Export(typeof(ILanguageClient))]
    public sealed class DracoLanguageClient : ILanguageClient
    {
        public string Name => "Draco Language Extension";
        public IEnumerable<string> ConfigurationSections => null;
        public object InitializationOptions => null;
        public IEnumerable<string> FilesToWatch => null;
        public bool ShowNotificationOnInitializeFailed => true;

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync;

        public Task<Connection> ActivateAsync(CancellationToken token)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                FileName = "draco-langserver",
                Arguments = "run --stdio",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            var process = new Process()
            {
                StartInfo = processStartInfo,
            };

            if (!process.Start()) return Task.FromResult<Connection>(null);

            return Task.FromResult(new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream));
        }

        public async Task OnLoadedAsync() => await StartAsync?.InvokeAsync(this, EventArgs.Empty);

        public Task OnServerInitializedAsync() => Task.CompletedTask;

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState) =>
            Task.FromResult(new InitializationFailureContext()
            {
                // TODO
                FailureMessage = "ERROR",
            });
    }
}
