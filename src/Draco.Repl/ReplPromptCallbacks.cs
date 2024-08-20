using System.Threading.Tasks;
using System.Threading;
using System;
using PrettyPrompt;
using PrettyPrompt.Consoles;
using Draco.Compiler.Api.Scripting;
using System.Collections.Generic;
using PrettyPrompt.Highlighting;
using System.Linq;
using PrettyPrompt.Completion;
using PrettyPrompt.Documents;

namespace Draco.Repl;

internal sealed class ReplPromptCallbacks(Configuration configuration, ReplSession session) : PromptCallbacks
{
    protected override async Task<KeyPress> TransformKeyPressAsync(
        string text, int caret, KeyPress keyPress, CancellationToken cancellationToken)
    {
        // Incomplete prompt, just add newline
        if (keyPress.ConsoleKeyInfo.Key == ConsoleKey.Enter
         && keyPress.ConsoleKeyInfo.Modifiers == default
         && !ReplSession.IsCompleteEntry(text))
        {
            // NOTE: We could smart-indent here like CSharpRepl does
            return new(ConsoleKey.Insert.ToKeyInfo('\0', shift: true), Environment.NewLine);
        }

        return keyPress;
    }

    protected override async Task<IReadOnlyCollection<FormatSpan>> HighlightCallbackAsync(
        string text, CancellationToken cancellationToken) => SyntaxHighlighter
            .Highlight(text)
            .Select(t => new FormatSpan(t.Span.Start, t.Span.Length, configuration.SyntaxColors.Get(t.Color)))
            .ToList();

    protected override async Task<IReadOnlyList<CompletionItem>> GetCompletionItemsAsync(
        string text, int caret, TextSpan spanToBeReplaced, CancellationToken cancellationToken) => session
        .GetCompletions(text, caret)
        .Select(c => new CompletionItem(c.DisplayText))
        .ToList();
}
