using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using PrettyPrompt;
using PrettyPrompt.Consoles;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Repl;

/// <summary>
/// Represents the main loop of the REPL.
/// </summary>
internal sealed class Loop(Configuration configuration, IConsole console)
{
    // TODO: Temporary until we find out how we can inherit everything from the host
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    /// <summary>
    /// Runs the REPL loop.
    /// </summary>
    /// <returns>The task representing the operation.</returns>
    public async Task Run()
    {
        var session = new ReplSession([.. BclReferences]);
        session.AddImports(configuration.DefaultImports);

        await using var prompt = new Prompt(
            callbacks: new ReplPromptCallbacks(),
            configuration: new PromptConfiguration(
                prompt: configuration.GetFormattedPrompt()));

        while (true)
        {
            var promptResult = await prompt.ReadLineAsync().ConfigureAwait(false);
            if (!promptResult.IsSuccess) break;

            var replResult = session.Evaluate(promptResult.Text);
            this.PrintResult(replResult);
        }
    }

    private void PrintResult(ExecutionResult<object?> result)
    {
        if (result.Success)
        {
            console.Write(result.Value?.ToString());
            console.Write(Environment.NewLine);
        }
        else
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                console.Write(configuration.GetFormattedDiagnostic(diagnostic));
                console.Write(Environment.NewLine);
            }
        }
    }
}
