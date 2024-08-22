using System;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Scripting;
using PrettyPrompt;
using PrettyPrompt.Consoles;

namespace Draco.Repl;

/// <summary>
/// Callbacks for the REPL prompt.
/// </summary>
internal sealed class ReplPromptCallbacks : PromptCallbacks
{
    protected override Task<KeyPress> TransformKeyPressAsync(
        string text, int caret, KeyPress keyPress, CancellationToken cancellationToken)
    {
        // Incomplete prompt, just add newline
        if (keyPress.ConsoleKeyInfo.Key == ConsoleKey.Enter
         && keyPress.ConsoleKeyInfo.Modifiers == default
         && !ReplSession.IsCompleteEntry(text))
        {
            // NOTE: We could smart-indent here like CSharpRepl does
            return Task.FromResult(new KeyPress(ConsoleKey.Insert.ToKeyInfo('\0', shift: true), Environment.NewLine));
        }

        return Task.FromResult(keyPress);
    }
}
