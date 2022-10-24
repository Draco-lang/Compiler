using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

    public override string ToString() => Internal.Syntax.CodeParseTreePrinter.Print(this.Green);
    public string ToDebugString() => Internal.Syntax.DebugParseTreePrinter.Print(this.Green);
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

    // Plumbing code for green-red conversion
    // TODO: Can we reduce boilerplate?

    [return: NotNullIfNotNull(nameof(token))]
    private static Token? ToRed(ParseTree? parent, Internal.Syntax.Token? token) =>
        token is null ? null : new(parent, token);

    private static ImmutableArray<Token> ToRed(ParseTree? parent, ImmutableArray<Internal.Syntax.Token> elements) =>
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
