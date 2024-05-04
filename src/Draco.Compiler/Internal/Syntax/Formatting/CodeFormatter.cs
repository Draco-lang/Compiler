using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax.Formatting;

public static class CodeFormatter
{
    public static string Format(FormatterSettings settings, IReadOnlyList<TokenMetadata> metadatas)
    {
        FoldTooLongLine(metadatas, settings);
        var builder = new StringBuilder();
        var stateMachine = new LineStateMachine(string.Concat(metadatas[0].ScopeInfo.CurrentTotalIndent));

        stateMachine.AddToken(metadatas[0], settings, false);

        for (var x = 1; x < metadatas.Count; x++)
        {
            var metadata = metadatas[x];
            // we ignore multiline string newline tokens because we handle them in the string expression visitor.

            if (metadata.DoesReturnLine?.Value ?? false)
            {
                builder.Append(stateMachine);
                builder.Append(settings.Newline);
                stateMachine = new LineStateMachine(string.Concat(metadata.ScopeInfo.CurrentTotalIndent));
            }
            if (metadata.Kind.HasFlag(WhitespaceBehavior.ExtraNewline))
            {
                builder.Append(settings.Newline);
            }

            stateMachine.AddToken(metadata, settings, x == metadatas.Count - 1);
        }
        builder.Append(stateMachine);
        builder.Append(settings.Newline);
        return builder.ToString();
    }

    private static void FoldTooLongLine(IReadOnlyList<TokenMetadata> metadatas, FormatterSettings settings)
    {
        var stateMachine = new LineStateMachine(string.Concat(metadatas[0].ScopeInfo.CurrentTotalIndent));
        var currentLineStart = 0;
        List<Scope> foldedScopes = [];
        for (var x = 0; x < metadatas.Count; x++)
        {
            var curr = metadatas[x];
            if (curr.DoesReturnLine?.Value ?? false) // if it's a new line
            {
                // we recreate a state machine for the new line.
                stateMachine = new LineStateMachine(string.Concat(curr.ScopeInfo.CurrentTotalIndent));
                currentLineStart = x;
                foldedScopes.Clear();
            }

            stateMachine.AddToken(curr, settings, false);

            if (stateMachine.LineWidth <= settings.LineWidth) continue;

            // the line is too long...

            var folded = curr.ScopeInfo.Fold(); // folding can fail if there is nothing else to fold.
            if (folded != null)
            {
                x = currentLineStart - 1;
                foldedScopes.Add(folded);
                stateMachine.Reset();
                continue;
            }

            // we can't fold the current scope anymore, so we revert our folding, and we fold the previous scopes on the line.
            // there can be other strategy taken in the future, parametrable through settings.

            // first rewind and fold any "as soon as possible" scopes.
            for (var i = x - 1; i >= currentLineStart; i--)
            {
                var scope = metadatas[i].ScopeInfo;
                if (scope.IsMaterialized?.Value ?? false) continue;
                if (scope.FoldPriority != FoldPriority.AsSoonAsPossible) continue;
                var prevFolded = scope.Fold();
                if (prevFolded != null)
                {
                    ResetBacktracking();
                    continue;
                }
            }
            // there was no high priority scope to fold, we try to get the low priority then.
            for (var i = x - 1; i >= currentLineStart; i--)
            {
                var scope = metadatas[i].ScopeInfo;
                if (scope.IsMaterialized?.Value ?? false) continue;
                var prevFolded = scope.Fold();
                if (prevFolded != null)
                {
                    ResetBacktracking();
                    continue;
                }
            }

            // we couldn't fold any scope, we just give up.

            void ResetBacktracking()
            {
                foreach (var scope in foldedScopes)
                {
                    scope.IsMaterialized.Value = null;
                }
                foldedScopes.Clear();
                x = currentLineStart - 1;
            }
        }
    }
}
