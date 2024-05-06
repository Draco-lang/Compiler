using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.Compiler.Internal.Syntax.Formatting;

public sealed class FormatterEngine
{
    private readonly Disposable scopePopper;
    private readonly TokenMetadata[] tokensMetadata;
    private readonly FormatterSettings settings;

    public FormatterEngine(int tokenCount, FormatterSettings settings)
    {
        this.scopePopper = new Disposable(this);
        this.tokensMetadata = new TokenMetadata[tokenCount];
        this.Scope = new(null, settings, FoldPriority.Never, "");
        this.Scope.IsMaterialized.Value = true;
        this.settings = settings;
    }

    private class Disposable(FormatterEngine formatter) : IDisposable
    {
        public void Dispose() => formatter.PopScope();
    }

    public int CurrentIdx { get; private set; }
    public Scope Scope { get; private set; }

    private Scope? scopeForNextToken;

    public TokenMetadata[] TokensMetadata => this.tokensMetadata;

    public ref TokenMetadata PreviousToken => ref this.tokensMetadata[this.CurrentIdx - 1];
    public ref TokenMetadata CurrentToken => ref this.tokensMetadata[this.CurrentIdx];
    public ref TokenMetadata NextToken => ref this.tokensMetadata[this.CurrentIdx + 1];

    public void SetCurrentTokenInfo(WhitespaceBehavior kind, string text)
    {
        this.CurrentToken.ScopeInfo = this.Scope;
        this.CurrentToken.Kind |= kind; // may have been set before visiting for convenience.
        this.CurrentToken.Text ??= text; // same
        this.CurrentIdx++;
        if (this.scopeForNextToken != null)
        {
            this.Scope = this.scopeForNextToken;
            this.scopeForNextToken = null;
        }
    }

    private void PopScope() => this.Scope = this.Scope.Parent!;

    public IDisposable CreateScope(string indentation)
    {
        this.Scope = new Scope(this.Scope, this.settings, FoldPriority.Never, indentation);
        this.Scope.IsMaterialized.Value = true;
        return this.scopePopper;
    }

    public void CreateScope(string indentation, Action action)
    {
        using (this.CreateScope(indentation)) action();
    }

    public IDisposable CreateScopeAfterNextToken(string indentation)
    {
        this.scopeForNextToken = new Scope(this.Scope, this.settings, FoldPriority.Never, indentation);
        this.scopeForNextToken.IsMaterialized.Value = true;
        return this.scopePopper;
    }


    public IDisposable CreateMaterializableScope(string indentation, FoldPriority foldBehavior)
    {
        this.Scope = new Scope(this.Scope, this.settings, foldBehavior, indentation);
        return this.scopePopper;
    }

    public IDisposable CreateMaterializableScope(int indexOfLevelingToken, FoldPriority foldBehavior)
    {
        this.Scope = new Scope(this.Scope, this.settings, foldBehavior, (this.tokensMetadata, indexOfLevelingToken));
        return this.scopePopper;
    }

    public void CreateMaterializableScope(string indentation, FoldPriority foldBehavior, Action action)
    {
        using (this.CreateMaterializableScope(indentation, foldBehavior)) action();
    }

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
                // we do not clear this, because we can be in the middle of trying to make the line fit in the width.
            }

            stateMachine.AddToken(curr, settings, false);

            if (stateMachine.LineWidth <= settings.LineWidth)
            {
                // we clear the folded scope, because the line is complete and we won't need it anymore.
                if (x != metadatas.Count - 1 && (metadatas[x + 1].DoesReturnLine?.Value ?? false))
                {
                    foldedScopes.Clear();
                }
                continue;
            }

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
                    Backtrack();
                    goto continue2;
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
                    Backtrack();
                    goto continue2;
                }
            }

        continue2:

            // we couldn't fold any scope, we just give up.

            void Backtrack()
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
