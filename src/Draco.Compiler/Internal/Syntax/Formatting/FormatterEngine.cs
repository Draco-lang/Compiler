using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

public sealed class FormatterEngine
{
    private readonly TokenMetadata[] tokensMetadata;
    private readonly FormatterSettings settings;

    public FormatterEngine(int tokenCount, FormatterSettings settings)
    {
        this.tokensMetadata = new TokenMetadata[tokenCount];
        this.Scope = new(null, settings, FoldPriority.Never, "");
        this.Scope.IsMaterialized.Value = true;
        this.settings = settings;
    }

    public int CurrentIdx { get; private set; }
    public Scope Scope { get; private set; }

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
    }

    public DisposeAction CreateScope(string indentation)
    {
        this.Scope = new Scope(this.Scope, this.settings, FoldPriority.Never, indentation);
        this.Scope.IsMaterialized.Value = true;
        return new DisposeAction(() => this.Scope = this.Scope.Parent!);
    }

    public void CreateScope(string indentation, Action action)
    {
        using (this.CreateScope(indentation)) action();
    }

    public DisposeAction CreateMaterializableScope(string indentation, FoldPriority foldBehavior)
    {
        this.Scope = new Scope(this.Scope, this.settings, foldBehavior, indentation);
        return new DisposeAction(() => this.Scope = this.Scope.Parent!);
    }

    public DisposeAction CreateMaterializableScope(int indexOfLevelingToken, FoldPriority foldBehavior)
    {
        this.Scope = new Scope(this.Scope, this.settings, foldBehavior, (this.tokensMetadata, indexOfLevelingToken));
        return new DisposeAction(() => this.Scope = this.Scope.Parent!);
    }

    public void CreateMaterializableScope(string indentation, FoldPriority foldBehavior, Action action)
    {
        using (this.CreateMaterializableScope(indentation, foldBehavior)) action();
    }
}
