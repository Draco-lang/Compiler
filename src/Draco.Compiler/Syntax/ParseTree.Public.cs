using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax.Public;

[Draco.RedGreenTree.RedTree(typeof(Syntax.ParseTree))]
internal abstract partial record class ParseTree
{
    private readonly Syntax.ParseTree green;
    public ParseTree? Parent { get; }

    // Plumbing for ToRed
    // TODO: Can we reduce plumbing
    // TODO: That's not enough, it needs a parent! We need a public token type.
    [return: NotNullIfNotNull(nameof(token))]
    private static Token? ToRed(ParseTree? parent, Token? token) => token;
    private static ValueArray<Stmt> ToRed(ParseTree? parent, ValueArray<Syntax.ParseTree.Stmt> elements) =>
        elements.Select(t => (Stmt)ToRed(parent, t)).ToValueArray();
    private static ValueArray<Decl> ToRed(ParseTree? parent, ValueArray<Syntax.ParseTree.Decl> elements) =>
        elements.Select(t => (Decl)ToRed(parent, t)).ToValueArray();
    private static ValueArray<StringPart> ToRed(ParseTree? parent, ValueArray<Syntax.ParseTree.StringPart> elements) =>
        elements.Select(t => (StringPart)ToRed(parent, t)).ToValueArray();
    private static ValueArray<ComparisonElement> ToRed(
        ParseTree? parent,
        ValueArray<Syntax.ParseTree.ComparisonElement> elements) =>
        elements.Select(t => (ComparisonElement)ToRed(parent, t)).ToValueArray();
    private static Syntax.ParseTree.Enclosed<Syntax.ParseTree.PunctuatedList<FuncParam>> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.Enclosed<Syntax.ParseTree.PunctuatedList<Syntax.ParseTree.FuncParam>> enclosed) => new(
            ToRed(parent, enclosed.OpenToken),
            ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));
    private static Syntax.ParseTree.Enclosed<Syntax.ParseTree.PunctuatedList<Expr>> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.Enclosed<Syntax.ParseTree.PunctuatedList<Syntax.ParseTree.Expr>> enclosed) => new(
            ToRed(parent, enclosed.OpenToken),
            ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));
    private static Syntax.ParseTree.PunctuatedList<FuncParam> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.PunctuatedList<Syntax.ParseTree.FuncParam> elements) =>
        new(elements.Elements.Select(t => ToRed(parent, t)).ToValueArray());
    private static Syntax.ParseTree.Punctuated<FuncParam> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.Punctuated<Syntax.ParseTree.FuncParam> element) =>
        new((FuncParam)ToRed(parent, element.Value), ToRed(parent, element.Punctuation));
    private static Syntax.ParseTree.PunctuatedList<Expr> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.PunctuatedList<Syntax.ParseTree.Expr> elements) =>
        new(elements.Elements.Select(t => ToRed(parent, t)).ToValueArray());
    private static Syntax.ParseTree.Punctuated<Expr> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.Punctuated<Syntax.ParseTree.Expr> element) =>
        new((Expr)ToRed(parent, element.Value), ToRed(parent, element.Punctuation));
    private static ValueArray<Token> ToRed(ParseTree? parent, ValueArray<Token> tokens) =>
        tokens.Select(t => ToRed(parent, t)).ToValueArray();
    private static Syntax.ParseTree.Enclosed<Expr> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.Enclosed<Syntax.ParseTree.Expr> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            (Expr)ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));
    private static Syntax.ParseTree.Enclosed<BlockContents> ToRed(
        ParseTree? parent,
        Syntax.ParseTree.Enclosed<Syntax.ParseTree.BlockContents> enclosed) =>
        new(
            ToRed(parent, enclosed.OpenToken),
            (BlockContents)ToRed(parent, enclosed.Value),
            ToRed(parent, enclosed.CloseToken));
}
