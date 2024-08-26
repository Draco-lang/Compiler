using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using PrettyPrompt;
using PrettyPrompt.Consoles;

namespace Draco.Repl;

/// <summary>
/// Callbacks for the REPL prompt.
/// </summary>
internal sealed class ReplPromptCallbacks(Configuration configuration) : PromptCallbacks
{
    protected override Task<KeyPress> TransformKeyPressAsync(
        string text, int caret, KeyPress keyPress, CancellationToken cancellationToken)
    {
        // Incomplete prompt, just add newline
        if (keyPress.ConsoleKeyInfo.Key == ConsoleKey.Enter
         && keyPress.ConsoleKeyInfo.Modifiers == default
         && !ReplSession.IsCompleteEntry(text))
        {
            var indentationString = this.GetIndentationString(InferIndentationLevel(text));
            var inserted = string.Concat(Environment.NewLine, indentationString);
            return Task.FromResult(new KeyPress(ConsoleKey.Insert.ToKeyInfo('\0', shift: true), inserted));
        }

        return Task.FromResult(keyPress);
    }

    private string GetIndentationString(int level)
    {
        var result = new StringBuilder();
        for (var i = 0; i < level; ++i) result.Append(configuration.Indentation);
        return result.ToString();
    }

    private static int InferIndentationLevel(string text)
    {
        var tree = ReplSession.ParseScript(text);
        var tokens = tree.Root.Tokens;
        // Count the balance of {, }, ( and )
        var indentationLevel = 0;
        foreach (var token in tokens)
        {
            // We assume 0-length is missing
            if (token.Text.Length == 0) continue;
            switch (token.Kind)
            {
            case TokenKind.CurlyOpen:
            case TokenKind.ParenOpen:
                ++indentationLevel;
                break;
            case TokenKind.CurlyClose:
            case TokenKind.ParenClose:
                --indentationLevel;
                break;
            }
            // Make sure it's at least 0 all the time
            indentationLevel = Math.Max(0, indentationLevel);
        }
        return indentationLevel;
    }
}
