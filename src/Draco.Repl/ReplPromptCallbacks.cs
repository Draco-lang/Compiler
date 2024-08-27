using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Api.CodeCompletion;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using PrettyPrompt;
using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;
using PrettyPrompt.Documents;
using PrettyPrompt.Highlighting;
using CompletionItem = PrettyPrompt.Completion.CompletionItem;

namespace Draco.Repl;

/// <summary>
/// Callbacks for the REPL prompt.
/// </summary>
internal sealed class ReplPromptCallbacks(
    Configuration configuration,
    ReplSession session) : PromptCallbacks
{
    private readonly CompletionService completionService = CompletionService.CreateDefault();

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

    protected override Task<IReadOnlyCollection<FormatSpan>> HighlightCallbackAsync(
        string text, CancellationToken cancellationToken)
    {
        var script = this.MakeScript(text);
        var tree = script.Compilation.SyntaxTrees.Single();
        var semanticModel = script.Compilation.GetSemanticModel(tree);

        var highlighting = SyntaxHighlighter.Highlight(tree, semanticModel);

        var result = new List<FormatSpan>();
        foreach (var fragment in highlighting)
        {
            var format = configuration.SyntaxColors.Get(fragment.Color);
            var syntaxSpan = fragment.Syntax.Span;
            result.Add(new FormatSpan(syntaxSpan.Start + fragment.Span.Start, fragment.Span.Length, format));
        }

        return Task.FromResult<IReadOnlyCollection<FormatSpan>>(result);
    }

    protected override Task<IReadOnlyList<CompletionItem>> GetCompletionItemsAsync(
        string text, int caret, TextSpan spanToBeReplaced, CancellationToken cancellationToken)
    {
        var script = this.MakeScript(text);
        var tree = script.Compilation.SyntaxTrees.Single();
        var semanticModel = script.Compilation.GetSemanticModel(tree);

        var cursorPosition = tree.IndexToSyntaxPosition(caret);
        var tokenAtCaret = tree.Root
            .TraverseSubtreesAtCursorPosition(cursorPosition)
            .OfType<SyntaxToken>()
            .LastOrDefault();

        var completionItems = this.completionService.GetCompletions(tree, semanticModel, caret);

        var result = new List<CompletionItem>();
        foreach (var item in completionItems)
        {
            var replacementText = item.Edits[0].Text;

            if (tokenAtCaret is not null && tokenAtCaret.Text.Length > 0)
            {
                // We can filter by prefix here
                if (!replacementText.StartsWith(tokenAtCaret.Text, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var coloring = CompletionKindToSyntaxColoring(item.Kind);
            var format = configuration.SyntaxColors.Get(coloring);
            var completion = new CompletionItem(
                replacementText: replacementText,
                displayText: new FormattedString(replacementText, format));
            result.Add(completion);
        }
        return Task.FromResult<IReadOnlyList<CompletionItem>>(result);
    }

    private Script<object?> MakeScript(string text) => Script.Create(
        code: text,
        globalImports: session.GlobalImports,
        metadataReferences: session.MetadataReferences,
        previousCompilation: session.LastCompilation);

    private static SyntaxColoring CompletionKindToSyntaxColoring(CompletionKind kind) =>
        Enum.Parse<SyntaxColoring>(kind.ToString());
}
