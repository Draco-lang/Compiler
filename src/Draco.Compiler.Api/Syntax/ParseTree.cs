using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Api.Syntax;

// Utilities for public API
public abstract partial class ParseTree
{
    /// <summary>
    /// Parses the given tree into a <see cref="ParseTree"/>.
    /// </summary>
    /// <param name="source">The source to parse.</param>
    /// <returns>The parsed tree.</returns>
    public static ParseTree Parse(string source)
    {
        var srcReader = Internal.Syntax.SourceReader.From(source);
        var lexer = new Internal.Syntax.Lexer(srcReader);
        var tokenSource = Internal.Syntax.TokenSource.From(lexer);
        var parser = new Internal.Syntax.Parser(tokenSource);
        var cu = parser.ParseCompilationUnit();
        return ToRed(null, cu);
    }
}

/// <summary>
/// The base class for all nodes in the Draco parse-tree.
/// </summary>
[RedTree(typeof(Internal.Syntax.ParseTree))]
public abstract partial class ParseTree
{
    private readonly Internal.Syntax.ParseTree green;

    /// <summary>
    /// The parent of this node, if any.
    /// </summary>
    public ParseTree? Parent { get; }

    private string? fullText;
    /// <summary>
    /// The text this node was parsed from.
    /// </summary>
    public string FullText => this.fullText ??= Internal.Syntax.CodeParseTreePrinter.Print(this.Green);

    private Position? position;
    /// <summary>
    /// The position of the start of this node in the source text.
    /// </summary>
    public Position Position => this.position ??= this.ComputePosition();

    private Range? range;
    /// <summary>
    /// The range of this node in the source text.
    /// </summary>
    public Range Range => this.range ??= this.ComputeRange();

    /// <summary>
    /// All <see cref="Token"/>s that this subtree consists of.
    /// </summary>
    public IEnumerable<Token> Tokens => this.GetTokens();

    private Position? positionWithLeadingTrivia;
    internal Position PositionWithLeadingTrivia =>
        this.positionWithLeadingTrivia ??= this.ComputePositionWithLeadingTrivia();

    public override string ToString() => this.ComputeTextWithoutSurroundingTrivia();
    public string ToDebugString() => Internal.Syntax.DebugParseTreePrinter.Print(this.Green);
}

public abstract partial class ParseTree
{
    protected virtual string ComputeTextWithoutSurroundingTrivia()
    {
        var sb = new StringBuilder();
        // We simply print the text of all tokens except the first and last ones
        // For the first, we ignore leading trivia, for the last we ignore trailing trivia
        var lastTrailingTrivia = ImmutableArray<Token>.Empty;
        var tokensEnumerator = this.Tokens.GetEnumerator();
        // The first token just gets it's content printed
        // That ignores the leading trivia, trailing will only be printed if there are following tokens
        Debug.Assert(tokensEnumerator.MoveNext());
        var firstToken = tokensEnumerator.Current;
        sb.Append(firstToken.Text);
        lastTrailingTrivia = firstToken.TrailingTrivia;
        while (tokensEnumerator.MoveNext())
        {
            var token = tokensEnumerator.Current;
            // Last trailing trivia
            foreach (var t in lastTrailingTrivia) sb.Append(t.Text);
            // Leading trivia
            foreach (var t in token.LeadingTrivia) sb.Append(t.Text);
            // Content
            sb.Append(token.Text);
            // Trailing trivia
            lastTrailingTrivia = token.TrailingTrivia;
        }
        return sb.ToString();
    }

    protected virtual Position ComputePosition()
    {
        // To avoid double-computing the offset by the leading trivia,
        // we propagate the work to the first token
        var firstToken = this.Tokens.First();
        return firstToken.Position;
    }

    private Range ComputeRange()
    {
        var start = this.Position;
        var end = StepPositionByText(start, this.FullText);
        return new(start, end);
    }

    protected virtual IEnumerable<Token> GetTokens()
    {
        foreach (var child in this.Children)
        {
            foreach (var t in child.Tokens) yield return t;
        }
    }

    private Position ComputePositionWithLeadingTrivia()
    {
        var offset = new Position(0, 0);
        // If there is a parent, we offset by the previous nodes
        // The simplest way of that is to request the range of all previous nodes and remember the
        // last ones end that is not this node
        if (this.Parent is not null)
        {
            offset = this.Parent.PositionWithLeadingTrivia;
            foreach (var parentsChild in this.Parent.Children)
            {
                if (ReferenceEquals(this.green, parentsChild.Green)) break;
                offset = parentsChild.Range.End;
            }
        }
        return offset;
    }

    // NOTE: This might be a good general utility somewhere else?
    private static Position StepPositionByText(Position start, string text)
    {
        var currLine = start.Line;
        var currCol = start.Column;
        for (var i = 0; i < text.Length; ++i)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                // Either Windows or OS-X 9 style newlines
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    // Windows-style, eat extra char
                    ++i;
                }
                // Otherwise OS-X 9 style
                ++currLine;
                currCol = 0;
            }
            else if (ch == '\n')
            {
                // Unix-style newline
                ++currLine;
                currCol = 0;
            }
            else
            {
                // NOTE: We might not want to increment in all cases
                ++currCol;
            }
        }
        return new(Line: currLine, Column: currCol);
    }
}

public abstract partial class ParseTree
{
    public sealed partial class Token
    {
        protected override string ComputeTextWithoutSurroundingTrivia() => this.Text;

        protected override Position ComputePosition()
        {
            var position = this.ComputePositionWithLeadingTrivia();
            // We offset by the leading trivia
            foreach (var trivia in this.LeadingTrivia)
            {
                position = StepPositionByText(position, trivia.Text);
            }
            return position;
        }

        protected override IEnumerable<Token> GetTokens()
        {
            yield return this;
        }
    }
}

public abstract partial class ParseTree
{
    public readonly record struct Enclosed<T>(
        Token OpenToken,
        T Value,
        Token CloseToken);

    public readonly record struct Punctuated<T>(
        T Value,
        Token? Punctuation);

    public readonly record struct PunctuatedList<T>(
        ImmutableArray<Punctuated<T>> Elements);
}

public abstract partial class ParseTree
{
    // Plumbing code for green-red conversion
    // TODO: Can we reduce boilerplate?

    [return: NotNullIfNotNull(nameof(token))]
    private static Token? ToRed(ParseTree? parent, Internal.Syntax.ParseTree.Token? token) =>
        token is null ? null : new(parent, token);

    private static IEnumerable<ParseTree> ToRed(ParseTree? parent, IEnumerable<Internal.Syntax.ParseTree> elements) =>
        elements.Select(e => ToRed(parent, e));

    private static ImmutableArray<Token> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.ParseTree.Token> elements) =>
        elements.Select(e => ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<Decl> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.ParseTree.Decl> elements) =>
        elements.Select(e => (Decl)ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<Stmt> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.ParseTree.Stmt> elements) =>
        elements.Select(e => (Stmt)ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<ComparisonElement> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.ParseTree.ComparisonElement> elements) =>
        elements.Select(e => (ComparisonElement)ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<StringPart> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.ParseTree.StringPart> elements) =>
        elements.Select(e => (StringPart)ToRed(parent, e)).ToImmutableArray();

    private static Enclosed<PunctuatedList<FuncParam>> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.FuncParam>> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static PunctuatedList<FuncParam> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.FuncParam> elements) =>
        new(elements.Elements.Select(e => ToRed(parent, e)).ToImmutableArray());

    private static Punctuated<FuncParam> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.Punctuated<Internal.Syntax.ParseTree.FuncParam> punctuated) =>
        new(
            (FuncParam)ToRed(parent, punctuated.Value),
            ToRed(parent, punctuated.Punctuation));

    private static Enclosed<BlockContents> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.BlockContents> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            (BlockContents)ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static Enclosed<Expr> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.Expr> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            (Expr)ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static Enclosed<PunctuatedList<Expr>> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.Expr>> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static PunctuatedList<Expr> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.Expr> elements) =>
        new(elements.Elements.Select(e => ToRed(parent, e)).ToImmutableArray());

    private static Punctuated<Expr> ToRed(ParseTree? parent, Internal.Syntax.ParseTree.Punctuated<Internal.Syntax.ParseTree.Expr> punctuated) =>
        new(
            (Expr)ToRed(parent, punctuated.Value),
            ToRed(parent, punctuated.Punctuation));
}
