using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Draco.Compiler.Api.Diagnostics;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Api.Syntax;

// Utilities for public API
public abstract partial class ParseNode
{
    /// <summary>
    /// Parses the given tree into a <see cref="ParseNode"/>.
    /// </summary>
    /// <param name="source">The source to parse.</param>
    /// <returns>The parsed tree.</returns>
    public static ParseNode Parse(string source)
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
[RedTree(typeof(Internal.Syntax.ParseNode))]
public abstract partial class ParseNode : IEquatable<ParseNode>
{
    private readonly Internal.Syntax.ParseNode green;

    /// <summary>
    /// The parent of this node, if any.
    /// </summary>
    public ParseNode? Parent { get; }

    /// <summary>
    /// The text this node was parsed from.
    /// </summary>
    public string TextIncludingTrivia => Internal.Syntax.CodeParseTreePrinter.Print(this.Green);

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
    /// The location of this node.
    /// </summary>
    public Location Location => new Location.InFile(this.Range);

    /// <summary>
    /// All <see cref="Token"/>s that this subtree consists of.
    /// </summary>
    public IEnumerable<Token> Tokens => this.GetTokens();

    /// <summary>
    /// Retrieves all syntactic <see cref="Diagnostic"/>s within this tree.
    /// </summary>
    /// <returns>The diagnostics inside this tree.</returns>
    public IEnumerable<Diagnostic> GetAllDiagnostics() =>
        this.CollectAllDiagnostics();

    private string? text;
    public override string ToString() => this.text ??= this.ComputeTextWithoutSurroundingTrivia();
    public string ToDebugString() => Internal.Syntax.DebugParseTreePrinter.Print(this.Green);
    public string ToDotGraphString() => Internal.Syntax.DotParseTreePrinter.Print(this);

    // Equality by green nodes
    public bool Equals(ParseNode? other) => ReferenceEquals(this.Green, other?.Green);
    public override bool Equals(object? obj) => this.Equals(obj as ParseNode);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.Green);

    public sealed partial class Token
    {
        public TokenType Type => this.Green.Type;
    }

    /// <summary>
    /// Formats the <see cref="ParseNode"/>.
    /// </summary>
    /// <returns>The formatted <see cref="ParseNode"/>.</returns>
    public ParseNode Format() => ToRed(
        parent: null,
        green: new Internal.Syntax.ParseTreeFormatter(Internal.Syntax.ParseTreeFormatterSettings.Default).Format(this.Green));
}

// Traverasal
public abstract partial class ParseNode
{
    /// <summary>
    /// Traverses this subtree in an in-order fashion, meaning that the order is root, left, right recursively.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{ParseNode}"/> that gives back nodes in order.</returns>
    public IEnumerable<ParseNode> InOrderTraverse()
    {
        yield return this;
        foreach (var child in this.Children)
        {
            foreach (var e in child.InOrderTraverse()) yield return e;
        }
    }

    /// <summary>
    /// Searches for a child node of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">The type of child to search for.</typeparam>
    /// <param name="index">The index of the child to search for.</param>
    /// <returns>The <paramref name="index"/>th child of type <typeparamref name="TNode"/>.</returns>
    public TNode FindInChildren<TNode>(int index = 0)
        where TNode : ParseNode => this
        .InOrderTraverse()
        .OfType<TNode>()
        .ElementAt(index);
}

public abstract partial class ParseNode
{
    internal Range TranslateRelativeRange(Internal.Diagnostics.RelativeRange range)
    {
        var text = this.ToString().AsSpan();
        var start = StepPositionByText(this.Range.Start, text[..range.Offset]);
        var minWidth = Math.Min(range.Width, text.Length);
        var end = StepPositionByText(start, text.Slice(range.Offset, minWidth));
        return new(start, end);
    }

    private IEnumerable<Diagnostic> CollectAllDiagnostics()
    {
        // Translate the diagnostics on this node
        foreach (var diag in this.Green.Diagnostics) yield return diag.ToApiDiagnostic(this);

        // Find in children too
        foreach (var diag in this.Children.SelectMany(c => c.CollectAllDiagnostics())) yield return diag;
    }

    protected virtual string ComputeTextWithoutSurroundingTrivia()
    {
        var sb = new StringBuilder();
        // We simply print the text of all tokens except the first and last ones
        // For the first, we ignore leading trivia, for the last we ignore trailing trivia
        var lastTrailingTrivia = ImmutableArray<Trivia>.Empty;
        using var tokenEnumerator = this.Tokens.GetEnumerator();
        // The first token just gets it's content printed
        // That ignores the leading trivia, trailing will only be printed if there are following tokens
        var hasFirstToken = tokenEnumerator.MoveNext();
        if (!hasFirstToken) return string.Empty;
        var firstToken = tokenEnumerator.Current;
        sb.Append(firstToken.Text);
        lastTrailingTrivia = firstToken.TrailingTrivia;
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
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
        // For simplicity we try to propagte to the first token
        var firstToken = this.Tokens.FirstOrDefault();
        if (firstToken is not null) return firstToken.Position;
        // If we can't, we try to do so with the end of the last token
        var precedingToken = this.GetPrecedingToken(this);
        if (precedingToken is not null) return precedingToken.Range.End;
        // Otherwise just default to 0, 0
        return new(0, 0);
    }

    private Range ComputeRange()
    {
        var start = this.Position;
        var end = StepPositionByText(start, this.ToString());
        return new(start, end);
    }

    protected virtual IEnumerable<Token> GetTokens() => this.Children.SelectMany(c => c.Tokens);

    private Token? GetPrecedingToken(ParseNode tree)
    {
        var preceding = null as Token;
        foreach (var child in this.Children)
        {
            if (ReferenceEquals(child.Green, tree.Green)) break;
            preceding = child.Tokens.LastOrDefault() ?? preceding;
        }
        if (preceding is not null) return preceding;
        if (this.Parent is not null) return this.Parent.GetPrecedingToken(tree);
        return null;
    }

    protected Token? GetPrecedingToken(Token token)
    {
        var preceding = null as Token;
        foreach (var t in this.Tokens)
        {
            if (ReferenceEquals(t.Green, token.Green)) break;
            preceding = t;
        }
        if (preceding is not null) return preceding;
        if (this.Parent is not null) return this.Parent.GetPrecedingToken(token);
        return null;
    }

    // NOTE: This might be a good general utility somewhere else?
    private static Position StepPositionByText(Position start, ReadOnlySpan<char> text)
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

public abstract partial class ParseNode
{
    public sealed partial class Token
    {
        protected override string ComputeTextWithoutSurroundingTrivia() => this.Text;

        protected override Position ComputePosition()
        {
            var position = new Position(Line: 0, Column: 0);
            // Just get the position of the preceding token, if there is one
            // If so, we offset from the end that range by the trailing trivia
            var precedingToken = this.GetPrecedingToken(this);
            if (precedingToken is not null)
            {
                position = precedingToken.Range.End;
                foreach (var t in precedingToken.TrailingTrivia)
                {
                    position = StepPositionByText(position, t.Text);
                }
            }
            // Offset by leading trivia
            foreach (var t in this.LeadingTrivia)
            {
                position = StepPositionByText(position, t.Text);
            }
            // We are done
            return position;
        }

        protected override IEnumerable<Token> GetTokens()
        {
            yield return this;
        }
    }
}

public abstract partial class ParseNode
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

public abstract partial class ParseNode
{
    // Plumbing code for green-red conversion
    // TODO: Can we reduce boilerplate?

    [return: NotNullIfNotNull(nameof(token))]
    internal static Token? ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Token? token) =>
        token is null ? null : new(parent, token);

    private static Trivia ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Trivia trivia) =>
        new(parent, trivia);

    private static IEnumerable<ParseNode> ToRed(ParseNode? parent, IEnumerable<Internal.Syntax.ParseNode> elements) =>
        elements.Select(e => ToRed(parent, e));

    private static ImmutableArray<ParseNode> ToRed(ParseNode? parent, ImmutableArray<Internal.Syntax.ParseNode> elements) =>
        elements.Select(e => ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<Token> ToRed(ParseNode? parent, ImmutableArray<Internal.Syntax.ParseNode.Token> elements) =>
        elements.Select(e => ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<Trivia> ToRed(ParseNode? parent, ImmutableArray<Internal.Syntax.ParseNode.Trivia> elements) =>
        elements.Select(e => ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<Decl> ToRed(ParseNode? parent, ImmutableArray<Internal.Syntax.ParseNode.Decl> elements) =>
        elements.Select(e => (Decl)ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<Stmt> ToRed(ParseNode? parent, ImmutableArray<Internal.Syntax.ParseNode.Stmt> elements) =>
        elements.Select(e => (Stmt)ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<ComparisonElement> ToRed(ParseNode? parent, ImmutableArray<Internal.Syntax.ParseNode.ComparisonElement> elements) =>
        elements.Select(e => (ComparisonElement)ToRed(parent, e)).ToImmutableArray();

    private static ImmutableArray<StringPart> ToRed(ParseNode? parent, ImmutableArray<Internal.Syntax.ParseNode.StringPart> elements) =>
        elements.Select(e => (StringPart)ToRed(parent, e)).ToImmutableArray();

    private static Enclosed<PunctuatedList<FuncParam>> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.FuncParam>> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static PunctuatedList<FuncParam> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.FuncParam> elements) =>
        new(elements.Elements.Select(e => ToRed(parent, e)).ToImmutableArray());

    private static Punctuated<FuncParam> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Punctuated<Internal.Syntax.ParseNode.FuncParam> punctuated) =>
        new(
            (FuncParam)ToRed(parent, punctuated.Value),
            ToRed(parent, punctuated.Punctuation));

    private static Enclosed<BlockContents> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.BlockContents> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            (BlockContents)ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static Enclosed<Expr> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.Expr> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            (Expr)ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static Enclosed<PunctuatedList<Expr>> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.Expr>> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));

    private static PunctuatedList<Expr> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.Expr> elements) =>
        new(elements.Elements.Select(e => ToRed(parent, e)).ToImmutableArray());

    private static Punctuated<Expr> ToRed(ParseNode? parent, Internal.Syntax.ParseNode.Punctuated<Internal.Syntax.ParseNode.Expr> punctuated) =>
        new(
            (Expr)ToRed(parent, punctuated.Value),
            ToRed(parent, punctuated.Punctuation));
}
