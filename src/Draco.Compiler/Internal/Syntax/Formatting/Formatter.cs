using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class Formatter : Api.Syntax.SyntaxVisitor
{
    private TokenDecoration[] tokenDecorations = [];
    private int currentIdx;
    private readonly Stack<ScopeInfo> scopes = new();
    private readonly SyntaxTree tree;
    private ref TokenDecoration CurrentToken => ref this.tokenDecorations[this.currentIdx];

    private ScopeInfo CurrentScope => this.scopes.Peek();

    public static async SolverTask<string> GetIndentation(IReadOnlyCollection<ScopeInfo> scopes)
    {
        var indentation = "";
        foreach (var scope in scopes)
        {
            var isMaterialized = await scope.IsMaterialized.Collapsed;
            if (isMaterialized)
            {
                indentation += scope.Indentation;
            }
        }
        return indentation;
    }

    /// <summary>
    /// Formats the given syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to format.</param>
    /// <param name="settings">The formatter settings to use.</param>
    /// <returns>The formatted tree.</returns>
    public static string Format(SyntaxTree tree, FormatterSettings? settings = null)
    {
        settings ??= FormatterSettings.Default;

        var formatter = new Formatter(settings, tree);

        // Construct token sequence
        tree.Root.Accept(formatter);

        var builder = new StringBuilder();
        var i = 0;
        foreach (var node in tree.PreOrderTraverse())
        {
            if (node is not Api.Syntax.SyntaxToken token)
                continue;
            var decoration = formatter.tokenDecorations[i];

            if (decoration.Indentation is not null) builder.Append(decoration.Indentation.Result);
            builder.Append(token.Text);
            builder.Append(decoration.RightPadding);
            if (decoration.DoesReturnLineCollapsible is not null)
            {
                builder.Append(decoration.DoesReturnLineCollapsible.Collapsed.Result ? "\n" : ""); // will default to false if not collapsed, that what we want.
            }
            i++;
        }
        return builder.ToString();
    }

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private Formatter(FormatterSettings settings, SyntaxTree tree)
    {
        this.Settings = settings;
        this.tree = tree;
    }

    public override void VisitCompilationUnit(Api.Syntax.CompilationUnitSyntax node)
    {
        this.tokenDecorations = new TokenDecoration[node.Tokens.Count()];
        base.VisitCompilationUnit(node);
    }

    public override void VisitSyntaxToken(Api.Syntax.SyntaxToken node)
    {
        switch (node.Kind)
        {
        case TokenKind.Minus:
        case TokenKind.Plus:
        case TokenKind.Assign:
        case TokenKind.KeywordMod:
            this.CurrentToken.SetWhitespace();
            this.CurrentToken.Indentation = SolverTask.FromResult(" ");
            break;
        case TokenKind.KeywordVar:
        case TokenKind.KeywordVal:
        case TokenKind.KeywordFunc:
        case TokenKind.KeywordReturn:
        case TokenKind.KeywordGoto:
        case TokenKind.Colon:
        case TokenKind.KeywordWhile:
            this.CurrentToken.SetWhitespace();
            break;
        }
        base.VisitSyntaxToken(node);
        this.currentIdx++;
    }

    public override void VisitStringExpression(Api.Syntax.StringExpressionSyntax node)
    {
        if (node.OpenQuotes.Kind != TokenKind.MultiLineStringStart)
        {
            base.VisitStringExpression(node);
            return;
        }
        this.CurrentToken.DoesReturnLineCollapsible = CollapsibleBool.Create(true);
        var i = 0;
        for (; i < node.Parts.Count; i++)
        {
            if (node.Parts[i].Children.SingleOrDefault() is Api.Syntax.SyntaxToken and { Kind: TokenKind.StringNewline })
            {
                continue;
            }
            ref var currentToken = ref this.tokenDecorations[this.currentIdx + i + 1];
            currentToken.Indentation = GetIndentation(this.scopes.ToArray());
        }
        this.tokenDecorations[this.currentIdx + i].SetNewline();
        using var _ = this.CreateScope(this.Settings.Indentation, true);
        this.tokenDecorations[this.currentIdx + i + 1].Indentation = GetIndentation(this.scopes.ToArray());
        base.VisitStringExpression(node);
    }

    public override void VisitTextStringPart(Api.Syntax.TextStringPartSyntax node)
    {
        base.VisitTextStringPart(node);
    }

    public override void VisitMemberExpression(Api.Syntax.MemberExpressionSyntax node)
    {
        base.VisitMemberExpression(node);
        this.CurrentScope.ItemsCount.Add(1);
    }

    public override void VisitSeparatedSyntaxList<TNode>(Api.Syntax.SeparatedSyntaxList<TNode> node)
    {
        base.VisitSeparatedSyntaxList(node);
        this.CurrentToken.SetWhitespace();
    }

    public override void VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        using var _ = this.CreateScope(this.Settings.Indentation, true);
        this.CurrentScope.IsMaterialized.Collapse(true);
        this.CurrentToken.SetNewline();
        base.VisitBlockFunctionBody(node);
    }


    public override void VisitCallExpression(Api.Syntax.CallExpressionSyntax node)
    {
        using var _ = this.CreateScope(this.Settings.Indentation, false);
        base.VisitCallExpression(node);
    }

    public override void VisitStatement(Api.Syntax.StatementSyntax node)
    {
        base.VisitStatement(node);
        ref var firstToken = ref this.tokenDecorations[this.currentIdx];
        firstToken.DoesReturnLineCollapsible = CollapsibleBool.Create(false);
        firstToken.Indentation = GetIndentation(this.scopes.ToArray());

        var endIdx = this.currentIdx + node.Tokens.Count() - 1;
        ref var lastToken = ref this.tokenDecorations[endIdx];
        lastToken.DoesReturnLineCollapsible = CollapsibleBool.Create(true);
        lastToken.Indentation = SolverTask.FromResult("");
    }

    private IDisposable CreateScope(string indentation, bool tangible)
    {
        var scope = new ScopeInfo(indentation);
        if (tangible) scope.IsMaterialized.Collapse(true);
        this.scopes.Push(scope);
        return new DisposeAction(() => this.scopes.Pop());
    }
}

internal class DisposeAction(Action action) : IDisposable
{
    public void Dispose() => action();
}

internal class ScopeInfo(string indentation)
{
    private readonly SolverTaskCompletionSource<Unit> _stableTcs = new();
    public SolverTask<Unit> WhenStable => this._stableTcs.Task;
    /// <summary>
    /// Represent if the scope is materialized or not.
    /// An unmaterialized scope is a potential scope, which is not folded yet.
    /// <code>items.Select(x => x).ToList()</code> have an unmaterialized scope.
    /// It can be materialized like:
    /// <code>
    /// items
    ///     .Select(x => x)
    ///     .ToList()
    /// </code>
    /// </summary>
    public CollapsibleBool IsMaterialized { get; } = CollapsibleBool.Create();
    public CollapsibleInt ItemsCount { get; } = CollapsibleInt.Create();
    public string Indentation { get; } = indentation;
}


internal static class TokenDecorationExtensions
{
    public static void SetNewline(this ref TokenDecoration decoration) => decoration.DoesReturnLineCollapsible = CollapsibleBool.Create(true);
    public static void SetWhitespace(this ref TokenDecoration decoration) => decoration.RightPadding = " ";

}

internal struct TokenDecoration
{
    private string? rightPadding;
    private SolverTask<string>? indentation;
    private CollapsibleBool? doesReturnLineCollapsible;

    [DisallowNull]
    public CollapsibleBool? DoesReturnLineCollapsible
    {
        readonly get => this.doesReturnLineCollapsible;
        set
        {
            if (value.Equals(this.doesReturnLineCollapsible)) return;
            if (this.doesReturnLineCollapsible is not null) throw new InvalidOperationException("DoesReturnLineCollapsible already set.");
            this.doesReturnLineCollapsible = value;
        }
    }

    [DisallowNull]
    public SolverTask<string>? Indentation
    {
        readonly get => this.indentation;
        set
        {
            if (this.indentation is not null)
            {
                if (this.indentation.IsCompleted && value.IsCompleted && this.indentation.Result == value.Result) return;
                throw new InvalidOperationException("Indentation already set.");
            }
            this.indentation = value;
        }
    }

    public string? RightPadding
    {
        readonly get => this.rightPadding;
        set
        {
            if (this.rightPadding is not null) throw new InvalidOperationException("Right padding already set.");
            this.rightPadding = value;
        }
    }
}


internal class CollapsibleBool : IEquatable<CollapsibleBool>
{
    private readonly SolverTaskCompletionSource<bool>? tcs;
    private readonly SolverTask<bool> task;

    private CollapsibleBool(SolverTaskCompletionSource<bool> tcs)
    {
        this.tcs = tcs;
        this.task = tcs.Task;
    }
    private CollapsibleBool(SolverTask<bool> task)
    {
        this.task = task;
    }

    public static CollapsibleBool Create() => new(new SolverTaskCompletionSource<bool>());
    public static CollapsibleBool Create(bool value) => new(SolverTask.FromResult(value));

    public void Collapse(bool collapse) => this.tcs?.SetResult(collapse);
    public bool Equals(CollapsibleBool? other)
    {
        if (other is null) return false;

        if (this.tcs is null)
        {
            if (other.tcs is not null) return false;
            return this.task.Result == other.task.Result;
        }
        if (other.tcs is null) return false;
        if (this.tcs.IsCompleted && other.tcs.IsCompleted) return this.task.Result == other.task.Result;
        return false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is CollapsibleBool collapsibleBool) return this.Equals(collapsibleBool);
        return false;
    }

    public SolverTask<bool> Collapsed => this.task;
}

internal class CollapsibleInt
{
    private readonly SolverTaskCompletionSource<int>? tcs;
    private readonly SolverTask<int> task;
    private int MinimumCurrentValue;
    private CollapsibleInt(SolverTaskCompletionSource<int> tcs)
    {
        this.tcs = tcs;
        this.task = tcs.Task;
    }

    private CollapsibleInt(SolverTask<int> task)
    {
        this.task = task;
    }

    public static CollapsibleInt Create() => new(new SolverTaskCompletionSource<int>());
    public static CollapsibleInt Create(int value) => new(SolverTask.FromResult(value));


    // order by desc
    private List<(int Value, SolverTaskCompletionSource<Unit> Tcs)>? _whenTcs;

    public void Add(int toAdd)
    {
        this.MinimumCurrentValue += toAdd;
        if (this._whenTcs is null) return;
        var i = this._whenTcs.Count;
        for (; i >= 0; i--)
        {
            var (value, tcs) = this._whenTcs![i];
            if (this.MinimumCurrentValue < value) break;
            tcs.SetResult(new Unit());
        }
        this._whenTcs.RemoveRange(i, this._whenTcs.Count - i + 1);
    }

    public void Collapse() => this.tcs?.SetResult(this.MinimumCurrentValue);

    public SolverTask<int> Collapsed => this.task;

    public SolverTask<Unit> WhenReaching(int number)
    {
        if (this.MinimumCurrentValue >= number) return SolverTask.FromResult(new Unit());
        this._whenTcs ??= [];
        var index = this._whenTcs.BinarySearch((number, null!), Comparer.Instance);
        if (index > 0) return this._whenTcs[index].Tcs.Task;
        var tcs = new SolverTaskCompletionSource<Unit>();
        this._whenTcs.Insert(~index, (number, tcs));
        return tcs.Task;
    }

    private class Comparer : IComparer<(int, SolverTaskCompletionSource<Unit>)>
    {
        public static Comparer Instance { get; } = new Comparer();
        // reverse comparison.
        public int Compare((int, SolverTaskCompletionSource<Unit>) x, (int, SolverTaskCompletionSource<Unit>) y) => y.Item1.CompareTo(x.Item1);
    }
}
