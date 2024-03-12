using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
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
    private readonly SyntaxTree tree; // debugging helper, to remove
    private ref TokenDecoration CurrentToken => ref this.tokenDecorations[this.currentIdx];

    private ScopeInfo CurrentScope => this.scopes.Peek();

    public static async SolverTask<string?> GetIndentation(IReadOnlyCollection<ScopeInfo> scopes)
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
        foreach (var token in tree.Root.Tokens)
        {
            if (token.Kind == TokenKind.StringNewline) continue;
            var decoration = formatter.tokenDecorations[i];

            if (decoration.DoesReturnLineCollapsible is not null)
            {
                builder.Append(decoration.DoesReturnLineCollapsible.Collapsed.Result ? settings.Newline : ""); // will default to false if not collapsed, that what we want.
            }
            if (decoration.Indentation is not null) builder.Append(decoration.Indentation.Result);
            builder.Append(decoration.LeftPadding);
            builder.Append(token.Text);
            builder.Append(decoration.RightPadding);
            i++;
        }
        builder.AppendLine();
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
        case TokenKind.StringNewline: // we ignore and don't render string newlines. our own indentation will take care of it.
            return;
        case TokenKind.Minus:
        case TokenKind.Plus:
        case TokenKind.Star:
        case TokenKind.Slash:
        case TokenKind.GreaterThan:
        case TokenKind.LessThan:
        case TokenKind.GreaterEqual:
        case TokenKind.LessEqual:
        case TokenKind.Equal:
        case TokenKind.Assign:
        case TokenKind.KeywordMod:
            this.CurrentToken.RightPadding = " ";
            this.CurrentToken.LeftPadding = " ";
            break;
        case TokenKind.KeywordVar:
        case TokenKind.KeywordVal:
        case TokenKind.KeywordFunc:
        case TokenKind.KeywordReturn:
        case TokenKind.KeywordGoto:
        case TokenKind.KeywordWhile:
        case TokenKind.KeywordIf:
        case TokenKind.KeywordImport:
            this.CurrentToken.RightPadding = " ";
            break;
        }
        base.VisitSyntaxToken(node);
        this.CurrentToken.TokenSize = node.Green.Width;
        this.currentIdx++;
    }

    public override void VisitStringExpression(Api.Syntax.StringExpressionSyntax node)
    {
        if (node.OpenQuotes.Kind != TokenKind.MultiLineStringStart)
        {
            base.VisitStringExpression(node);
            return;
        }
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
        using var _ = this.CreateScope(this.Settings.Indentation, true);
        this.tokenDecorations[this.currentIdx + i + 1].Indentation = GetIndentation(this.scopes.ToArray());
        base.VisitStringExpression(node);
    }


    public override void VisitMemberExpression(Api.Syntax.MemberExpressionSyntax node)
    {
        base.VisitMemberExpression(node);
        this.CurrentScope.ItemsCount.Add(1);
    }

    public override void VisitBlockFunctionBody(Api.Syntax.BlockFunctionBodySyntax node)
    {
        this.CurrentToken.LeftPadding = " ";
        this.CreateScope(this.Settings.Indentation, true, () => base.VisitBlockFunctionBody(node));
        this.tokenDecorations[this.currentIdx - 1].Indentation = GetIndentation(this.scopes.ToArray());
    }

    public override void VisitStatement(Api.Syntax.StatementSyntax node)
    {
        this.CurrentScope.ItemsCount.Add(1);

        if (node is Api.Syntax.DeclarationStatementSyntax { Declaration: Api.Syntax.LabelDeclarationSyntax })
        {
            this.CurrentToken.Indentation = GetIndentation(this.scopes.Skip(1).ToArray());
        }
        else if (node.Parent is Api.Syntax.BlockExpressionSyntax)
        {
            this.CurrentToken.Indentation = GetIndentation(this.scopes.ToArray());
        }
        else
        {
            async SolverTask<string?> Indentation()
            {
                var scope = this.CurrentScope;
                var haveMoreThanOneStatement = await scope.ItemsCount.WhenGreaterOrEqual(2);
                if (haveMoreThanOneStatement) return await GetIndentation(this.scopes.ToArray());
                return null;
            }
            this.CurrentToken.Indentation = Indentation();

        }
        base.VisitStatement(node);
    }

    public override void VisitWhileExpression(Api.Syntax.WhileExpressionSyntax node)
    {
        this.tokenDecorations[this.currentIdx + 2 + node.Condition.Tokens.Count()].RightPadding = " ";
        this.CreateScope(this.Settings.Indentation, false, () => base.VisitWhileExpression(node));
    }

    public override void VisitIfExpression(Api.Syntax.IfExpressionSyntax node)
    {
        this.tokenDecorations[this.currentIdx + 2 + node.Condition.Tokens.Count()].RightPadding = " ";
        this.CreateScope(this.Settings.Indentation, false, () =>
        {
            this.VisitExpression(node);
            node.IfKeyword.Accept(this);
            node.OpenParen.Accept(this);
            node.Condition.Accept(this);
            node.CloseParen.Accept(this);
            node.Then.Accept(this);
        });
        node.Else?.Accept(this);
    }

    public override void VisitElseClause(Api.Syntax.ElseClauseSyntax node)
    {
        this.CurrentToken.RightPadding = " ";
        if (node.Parent!.Parent is Api.Syntax.ExpressionStatementSyntax)
        {
            this.CurrentToken.Indentation = GetIndentation(this.scopes.ToArray());
        }
        else
        {
            this.CurrentToken.LeftPadding = " ";
        }
        this.CreateScope(this.Settings.Indentation, false, () => base.VisitElseClause(node));
    }

    public override void VisitBlockExpression(Api.Syntax.BlockExpressionSyntax node)
    {
        // this means we are in a if/while/else, and *can* create an indentation with a regular expression folding:
        // if (blabla) an expression;
        // it can fold:
        // if (blabla)
        //     an expression;
        // but since we are in a block we create our own scope and the if/while/else will never create it's own scope.
        if (!this.CurrentScope.IsMaterialized.Collapsed.IsCompleted)
        {
            this.CurrentScope.IsMaterialized.Collapse(false);
        }

        this.CreateScope(this.Settings.Indentation, true, () =>
        {
            this.VisitExpression(node);
            node.OpenBrace.Accept(this);
            node.Statements.Accept(this);
            if (node.Value != null)
            {
                this.CurrentToken.Indentation = GetIndentation(this.scopes.ToArray());
                node.Value.Accept(this);
            }
            node.CloseBrace.Accept(this);
        });
        this.tokenDecorations[this.currentIdx - 1].Indentation = GetIndentation(this.scopes.ToArray());
    }

    public override void VisitTypeSpecifier(Api.Syntax.TypeSpecifierSyntax node)
    {
        this.CurrentToken.RightPadding = " ";
        base.VisitTypeSpecifier(node);
    }

    public override void VisitDeclaration(Api.Syntax.DeclarationSyntax node)
    {
        this.tokenDecorations[this.currentIdx + node.Tokens.Count()].Indentation = GetIndentation(this.scopes.ToArray());
        base.VisitDeclaration(node);
    }

    private IDisposable CreateScope(string indentation, bool tangible)
    {
        var scope = new ScopeInfo(indentation);
        if (tangible) scope.IsMaterialized.Collapse(true);
        this.scopes.Push(scope);
        return new DisposeAction(() =>
        {
            var scope = this.scopes.Pop();
            scope.Dispose();
        });
    }
    private void CreateScope(string indentation, bool tangible, Action action)
    {
        using (this.CreateScope(indentation, tangible)) action();
    }



    private void FormatTooLongLine(int index)
    {
        async SolverTask<int> GetLineWidth(int index)
        {
            var sum = 0;
            while (index > 0)
            {
                index--;
                var tokenDecoration = this.tokenDecorations[index];
                sum += tokenDecoration.TokenSize;
                if (tokenDecoration.DoesReturnLineCollapsible is null) continue;
                var doesReturnLine = await tokenDecoration.DoesReturnLineCollapsible.Collapsed;
                if (doesReturnLine)
                {
                    sum += tokenDecoration.Indentation!.Result!.Length;
                    break;
                }
            }
            return sum;
        }

        async SolverTask<Unit> TrySplitLine(int index, int maxLine)
        {
            var width = await GetLineWidth(index);
            if (width <= maxLine) return default;

        }
    }
}

internal class DisposeAction(Action action) : IDisposable
{
    public void Dispose() => action();
}

internal class ScopeInfo(string indentation) : IDisposable
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

    public void Dispose() => this.ItemsCount.Collapse();
}

internal struct TokenDecoration
{
    private string? rightPadding;
    private string? leftPadding;
    private SolverTask<string?>? indentation;
    public int TokenSize { get; set; }
    public int TotalSize => this.TokenSize + (this.leftPadding?.Length ?? 0) + (this.rightPadding?.Length ?? 0);

    [DisallowNull]
    public CollapsibleBool? DoesReturnLineCollapsible { get; private set; }

    [DisallowNull]
    public SolverTask<string?>? Indentation
    {
        readonly get => this.indentation;
        set
        {
            if (this.indentation is not null)
            {
                if (this.indentation.IsCompleted && value.IsCompleted && this.indentation.Result == value.Result) return;
                throw new InvalidOperationException("Indentation already set.");
            }
            var doesReturnLine = this.DoesReturnLineCollapsible = CollapsibleBool.Create();
            this.indentation = value;
            var myThis = this;
            this.indentation.Awaiter.OnCompleted(() =>
            {
                doesReturnLine.Collapse(value.Result != null);
            });
        }
    }

    public string? LeftPadding
    {
        readonly get => this.leftPadding;
        set
        {
            if (this.leftPadding is not null) throw new InvalidOperationException("Left padding already set.");
            this.leftPadding = value;
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
    private List<(int Value, SolverTaskCompletionSource<bool> Tcs)>? _whenTcs;

    public void Add(int toAdd)
    {
        this.MinimumCurrentValue += toAdd;
        if (this._whenTcs is null) return;
        var i = this._whenTcs.Count - 1;
        if (i < 0) return;
        while (true)
        {
            var (value, tcs) = this._whenTcs![i];
            if (this.MinimumCurrentValue < value) break;
            tcs.SetResult(true);
            if (i == 0) break;
            i--;
        }
        this._whenTcs.RemoveRange(i, this._whenTcs.Count - i);
    }

    public void Collapse()
    {
        if (this._whenTcs is not null)
        {
            foreach (var (_, Tcs) in this._whenTcs ?? Enumerable.Empty<(int Value, SolverTaskCompletionSource<bool> Tcs)>())
            {
                Tcs.SetResult(false);
            }
            this._whenTcs = null;
        }

        this.tcs?.SetResult(this.MinimumCurrentValue);
    }

    public SolverTask<int> Collapsed => this.task;

    public SolverTask<bool> WhenGreaterOrEqual(int number)
    {
        if (this.MinimumCurrentValue >= number) return SolverTask.FromResult(true);
        this._whenTcs ??= [];
        var index = this._whenTcs.BinarySearch((number, null!), Comparer.Instance);
        if (index > 0) return this._whenTcs[index].Tcs.Task;
        var tcs = new SolverTaskCompletionSource<bool>();
        this._whenTcs.Insert(~index, (number, tcs));
        return tcs.Task;
    }

    private class Comparer : IComparer<(int, SolverTaskCompletionSource<bool>)>
    {
        public static Comparer Instance { get; } = new Comparer();
        // reverse comparison.
        public int Compare((int, SolverTaskCompletionSource<bool>) x, (int, SolverTaskCompletionSource<bool>) y) => y.Item1.CompareTo(x.Item1);
    }
}
