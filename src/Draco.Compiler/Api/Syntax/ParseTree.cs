using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using Draco.RedGreenTree.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
public abstract partial class ParseTree : IEquatable<ParseTree>
{
    private readonly Internal.Syntax.ParseTree green;

    /// <summary>
    /// The parent of this node, if any.
    /// </summary>
    public ParseTree? Parent { get; }

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
    public Location Location => new(this.Range);

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

    public override string ToString() => this.ComputeTextWithoutSurroundingTrivia();
    public string ToDebugString() => Internal.Syntax.DebugParseTreePrinter.Print(this.Green);
    public string ToDotGraphString() => Internal.Syntax.DotParseTreePrinter.Print(this);

    // Equality by green nodes
    public bool Equals(ParseTree? other) => ReferenceEquals(this.Green, other?.Green);
    public override bool Equals(object? obj) => this.Equals(obj as ParseTree);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this.Green);

    public sealed partial class Token
    {
        public TokenType Type => this.Green.Type;
    }
}

public abstract partial class ParseTree
{
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
        var lastTrailingTrivia = ImmutableArray<Token>.Empty;
        using var tokenEnumerator = this.Tokens.GetEnumerator();
        // The first token just gets it's content printed
        // That ignores the leading trivia, trailing will only be printed if there are following tokens
        var hasFirstToken = tokenEnumerator.MoveNext();
        Debug.Assert(hasFirstToken);
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

    private Token? GetPrecedingToken(ParseTree tree)
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
