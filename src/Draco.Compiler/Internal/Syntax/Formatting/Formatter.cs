using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax.Formatting;

static class MaybeWhitespaceExtension
{
    public static WhiteSpaceOrToken AsMaybe(this SyntaxToken token) => new(token, false, 0);
}

class WhiteSpaceOrToken
{
    public WhiteSpaceOrToken(SyntaxToken? token, bool HasReturnLine, int whiteSpaceCount)
    {
        this.Token = token;
        this.DoesReturnLine = HasReturnLine;
        this.WhiteSpaceCount = whiteSpaceCount;
    }

    public SyntaxToken? Token { get; }
    public virtual bool DoesReturnLine { get; set; }
    public int WhiteSpaceCount { get; set; }

    public static WhiteSpaceOrToken NewLine(int whiteSpaceCount) => new(null, true, whiteSpaceCount);
}

class MaybeWhiteSpace : WhiteSpaceOrToken
{
    public MaybeWhiteSpace(int whiteSpaceCount) : base(null, false, whiteSpaceCount)
    {
    }

    public bool Collapsed { get; set; }
}



internal sealed class Formatter : SyntaxVisitor
{
    private readonly List<WhiteSpaceOrToken> _tokens = new();
    private readonly Stack<int> _indents = new();
    private int CurrentWhiteSpaceCount => this._indents.Sum();

    /// <summary>
    /// Formats the given syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to format.</param>
    /// <param name="settings">The formatter settings to use.</param>
    /// <returns>The formatted tree.</returns>
    public static IEnumerable<SyntaxToken> Format(SyntaxTree tree, FormatterSettings? settings = null)
    {
        settings ??= FormatterSettings.Default;
        var formatter = new Formatter(settings);

        // Construct token sequence
        tree.GreenRoot.Accept(formatter);

    }

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
    }

    public override void VisitImportDeclaration(ImportDeclarationSyntax node)
    {
        this._tokens.AddRange(node.Tokens.Select(s => s.AsMaybe()));
        this._tokens.Add(WhiteSpaceOrToken.NewLine(this.CurrentWhiteSpaceCount));
    }

    public override void VisitCallExpression(CallExpressionSyntax node)
    {
        //this._tokens.Add(WhiteSpaceOrToken.NewLine(CurrentWhiteSpaceCount));
        base.VisitCallExpression(node);
    }
}

internal class CollapsibleBool
{
    private readonly SolverTaskCompletionSource<bool> tcs = new();

    public void Collapse(bool collapse) => this.tcs.SetResult(collapse);

    public SolverTask<bool> Collapsed => this.tcs.Task;
}

internal class CollapsibleInt(int CurrentValue)
{
    private readonly SolverTaskCompletionSource<int> tcs = new();

    // order by desc
    private List<(int Value, SolverTaskCompletionSource<Unit> Tcs)>? _whenTcs;

    public void Add(int toAdd)
    {
        CurrentValue += toAdd;
        if (this._whenTcs is null) return;
        var i = this._whenTcs.Count;
        for (; i >= 0; i--)
        {
            var (value, tcs) = this._whenTcs![i];
            if (CurrentValue < value) break;
            tcs.SetResult(new Unit());
        }
        this._whenTcs.RemoveRange(i, this._whenTcs.Count - i + 1);
    }

    public void Collapse() => this.tcs.SetResult(CurrentValue);

    public SolverTask<int> Collapsed => this.tcs.Task;

    public SolverTask<Unit> WhenReaching(int number)
    {
        if (CurrentValue >= number) return SolverTask.FromResult(new Unit());
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