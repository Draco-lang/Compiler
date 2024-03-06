using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class Formatter : Api.Syntax.SyntaxVisitor
{
    private readonly List<TokenDecoration> tokenDecorations = new();
    private readonly Stack<ScopeInfo> scopes = new();

    private TokenDecoration CurrentToken
    {
        get => this.tokenDecorations[^1];
        set => this.tokenDecorations[^1] = value;
    }

    private ScopeInfo CurrentScope => this.scopes.Peek();

    private void SetTokenDecoration(Func<TokenDecoration, SolverTask<string>> stableWhen) => this.tokenDecorations[^1] = TokenDecoration.Create(stableWhen);

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
        var formatter = new Formatter(settings);

        // Construct token sequence
        tree.Root.Accept(formatter);

        var builder = new StringBuilder();
        var i = 0;
        foreach (var node in tree.PreOrderTraverse())
        {
            if (node is not Api.Syntax.SyntaxToken token)
                continue;
            var decoration = formatter.tokenDecorations[i];

            if (decoration is not null) builder.Append(decoration.Indentation);
            builder.Append(token.Text);
            if (decoration is not null)
            {
                builder.Append(decoration.RightPadding);
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

    private Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
    }

    public override void VisitSyntaxToken(Api.Syntax.SyntaxToken node)
    {
        switch (node.Kind)
        {
        case TokenKind.Assign:
            this.tokenDecorations.Add(TokenDecoration.Create(token =>
            {
                token.RightPadding = " ";
                return SolverTask.FromResult(" ");
            }));
            break;
        case TokenKind.KeywordVar:
        case TokenKind.KeywordVal:
        case TokenKind.KeywordFunc:
        case TokenKind.KeywordReturn:
        case TokenKind.KeywordGoto:
            this.tokenDecorations.Add(TokenDecoration.Whitespace());
            break;
        default:
            this.tokenDecorations.Add(null!);
            break;
        }
        base.VisitSyntaxToken(node);
    }

    public override void VisitMemberExpression(Api.Syntax.MemberExpressionSyntax node)
    {
        base.VisitMemberExpression(node);
        this.CurrentScope.ItemsCount.Add(1);
    }

    public override void VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        using var _ = this.CreateScope(this.Settings.Indentation);
        var openIdx = this.tokenDecorations.Count;
        base.VisitBlockFunctionBody(node);
        Debug.Assert(this.tokenDecorations[openIdx] is null);
        Debug.Assert(this.CurrentToken is null);

        this.tokenDecorations[openIdx] = TokenDecoration.Create(decoration =>
        {
            decoration.DoesReturnLineCollapsible.Collapse(true);
            return SolverTask.FromResult(" ");
        });

        this.CurrentToken = TokenDecoration.Create(async decoration =>
        {
            decoration.DoesReturnLineCollapsible.Collapse(true);
            return await GetIndentation(this.scopes.Skip(1).ToArray());
        });
    }


    public override void VisitCallExpression(Api.Syntax.CallExpressionSyntax node)
    {
        using var _ = this.CreateScope(this.Settings.Indentation);
        base.VisitCallExpression(node);
    }

    public override void VisitBlockExpression(Api.Syntax.BlockExpressionSyntax node)
    {
        using var _ = this.CreateScope(this.Settings.Indentation);
        var idx = this.tokenDecorations.Count;
        base.VisitBlockExpression(node);
        Debug.Assert(this.tokenDecorations[idx] is null);
        Debug.Assert(this.CurrentToken is null);
        var task = TokenDecoration.Create(async decoration =>
        {
            decoration.DoesReturnLineCollapsible.Collapse(true);
            return await GetIndentation(this.scopes.Skip(1).ToArray());
        });
        this.tokenDecorations[idx] = task;
        this.CurrentToken = task;
    }

    public override void VisitStatement(Api.Syntax.StatementSyntax node)
    {
        var idx = this.tokenDecorations.Count;
        base.VisitStatement(node);
        this.tokenDecorations[idx] = TokenDecoration.Create(async token =>
        {
            token.DoesReturnLineCollapsible.Collapse(false);
            return await GetIndentation(this.scopes.Skip(1).ToArray());
        });
        this.CurrentToken = TokenDecoration.Create(token =>
        {
            token.DoesReturnLineCollapsible.Collapse(true);
            return SolverTask.FromResult("");
        });
    }

    private IDisposable CreateScope(string indentation)
    {
        var scope = new ScopeInfo(indentation);
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

internal class TokenDecoration
{
    private readonly SolverTaskCompletionSource<Unit> _stableTcs = new();

    public static TokenDecoration Create(Func<TokenDecoration, SolverTask<string>> stableWhen)
    {
        var tokenDecoration = new TokenDecoration();
        var awaiter = stableWhen(tokenDecoration).Awaiter;
        awaiter.OnCompleted(() =>
        {
            tokenDecoration.SetStable();
            tokenDecoration.Indentation = awaiter.GetResult();
        });
        return tokenDecoration;
    }

    public static TokenDecoration Whitespace() => Create(token =>
    {
        token.DoesReturnLineCollapsible.Collapse(false);
        token.RightPadding = " ";
        return SolverTask.FromResult("");
    });

    public CollapsibleBool DoesReturnLineCollapsible { get; } = CollapsibleBool.Create();

    public SolverTask<Unit> WhenStable => this._stableTcs.Task;
    public string Indentation { get; private set; } = "";
    public string RightPadding { get; set; } = "";

    private void SetStable() => this._stableTcs.SetResult(new Unit());
}


internal class CollapsibleBool
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
