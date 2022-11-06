using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// An immutable structure representing a parsed source text with information about concrete syntax.
/// </summary>
[GreenTree]
internal abstract partial record class ParseTree
{
    public abstract int Width { get; }
    public abstract IEnumerable<ParseTree> Children { get; }

    /// <summary>
    /// The diagnostics attached to this tree node.
    /// </summary>
    internal virtual ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;
}

// Traverasal
internal abstract partial record class ParseTree
{
    /// <summary>
    /// Traverses this subtree in an in-order fashion, meaning that the order is root, left, right recursively.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{ParseTree}"/> that gives back nodes in order.</returns>
    public IEnumerable<ParseTree> InOrderTraverse()
    {
        yield return this;
        foreach (var child in this.Children)
        {
            foreach (var e in child.InOrderTraverse()) yield return e;
        }
    }
}

// Nodes
internal partial record class ParseTree
{
    /// <summary>
    /// A node enclosed by two tokens.
    /// </summary>
    public readonly record struct Enclosed<T>(
        Token OpenToken,
        T Value,
        Token CloseToken);

    /// <summary>
    /// A single punctuated element.
    /// </summary>
    public readonly record struct Punctuated<T>(
        T Value,
        Token? Punctuation);

    /// <summary>
    /// A list of nodes with a token separating each element.
    /// </summary>
    public readonly record struct PunctuatedList<T>(
        ImmutableArray<Punctuated<T>> Elements);

    /// <summary>
    /// A compilation unit, the top-most node in the parse tree.
    /// </summary>
    public sealed partial record class CompilationUnit(
        ImmutableArray<Decl> Declarations,
        Token End) : ParseTree;

    /// <summary>
    /// A declaration, either top-level or as a statement.
    /// </summary>
    public abstract partial record class Decl : ParseTree
    {
        /// <summary>
        /// Unexpected input in declaration context.
        /// </summary>
        public sealed partial record class Unexpected(
            ImmutableArray<ParseTree> Elements,
            ImmutableArray<Diagnostic> Diagnostics) : Decl
        {
            /// <inheritdoc/>
            internal override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
        }

        /// <summary>
        /// A function declaration.
        /// </summary>
        public sealed partial record class Func(
            Token FuncKeyword,
            Token Identifier,
            Enclosed<PunctuatedList<FuncParam>> Params,
            TypeSpecifier? ReturnType,
            FuncBody Body) : Decl;

        /// <summary>
        /// A label declaration.
        /// </summary>
        public sealed partial record class Label(
            Token Identifier,
            Token ColonToken) : Decl;

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed partial record class Variable(
            Token Keyword, // Either var or val
            Token Identifier,
            TypeSpecifier? Type,
            ValueInitializer? Initializer,
            Token Semicolon) : Decl;
    }

    /// <summary>
    /// A function parameter.
    /// </summary>
    public sealed partial record class FuncParam(
        Token Identifier,
        TypeSpecifier Type) : ParseTree;

    /// <summary>
    /// A value initializer construct.
    /// </summary>
    public sealed partial record class ValueInitializer(
        Token AssignToken,
        Expr Value) : ParseTree;

    /// <summary>
    /// A function body, either a block or in-line.
    /// </summary>
    public abstract partial record class FuncBody : ParseTree
    {
        /// <summary>
        /// Unexpected input in function body context.
        /// </summary>
        public sealed partial record class Unexpected(
            ImmutableArray<ParseTree> Elements,
            ImmutableArray<Diagnostic> Diagnostics) : FuncBody
        {
            /// <inheritdoc/>
            internal override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
        }

        /// <summary>
        /// A block function body.
        /// </summary>
        public sealed partial record class BlockBody(
            Expr.Block Block) : FuncBody;

        /// <summary>
        /// An in-line function body.
        /// </summary>
        public sealed partial record class InlineBody(
            Token AssignToken,
            Expr Expression,
            Token Semicolon) : FuncBody;
    }

    /// <summary>
    /// A type expression, i.e. a reference to a type.
    /// </summary>
    public abstract partial record class TypeExpr : ParseTree
    {
        // This is the only kind of type expression for now
        public sealed partial record class Name(
            Token Identifier) : TypeExpr;
    }

    /// <summary>
    /// A type specifier for functions, variables, expressions, etc.
    /// </summary>
    public sealed partial record class TypeSpecifier(
        Token ColonToken,
        TypeExpr Type) : ParseTree;

    /// <summary>
    /// A statement in a block.
    /// </summary>
    public abstract partial record class Stmt : ParseTree
    {
        /// <summary>
        /// Unexpected input in statement context.
        /// </summary>
        public sealed partial record class Unexpected(
            ImmutableArray<ParseTree> Elements,
            ImmutableArray<Diagnostic> Diagnostics) : Stmt
        {
            /// <inheritdoc/>
            internal override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
        }

        /// <summary>
        /// A declaration statement.
        /// </summary>
        public new sealed partial record class Decl(
            ParseTree.Decl Declaration) : Stmt;

        /// <summary>
        /// An expression statement.
        /// </summary>
        public new sealed partial record class Expr(
            ParseTree.Expr Expression,
            Token? Semicolon) : Stmt;
    }

    /// <summary>
    /// An expression.
    /// </summary>
    public abstract partial record class Expr : ParseTree
    {
        /// <summary>
        /// Unexpected input in expression context.
        /// </summary>
        public sealed partial record class Unexpected(
            ImmutableArray<ParseTree> Elements,
            ImmutableArray<Diagnostic> Diagnostics) : Expr
        {
            /// <inheritdoc/>
            internal override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
        }

        /// <summary>
        /// An expression that results in unit type and only executes a statement.
        /// </summary>
        public sealed partial record class UnitStmt(
            Stmt Statement) : Expr;

        /// <summary>
        /// A block of statements and an optional value.
        /// </summary>
        public sealed partial record class Block(
            Enclosed<BlockContents> Enclosed) : Expr;

        /// <summary>
        /// An if-expression with an option else clause.
        /// </summary>
        public sealed partial record class If(
            Token IfKeyword,
            Enclosed<Expr> Condition,
            Expr Then,
            ElseClause? Else) : Expr;

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed partial record class While(
            Token WhileKeyword,
            Enclosed<Expr> Condition,
            Expr Expression) : Expr;

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed partial record class Goto(
            Token GotoKeyword,
            Expr.Name Target) : Expr;

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed partial record class Return(
            Token ReturnKeyword,
            Expr? Expression) : Expr;

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed partial record class Literal(
            Token Value) : Expr;

        /// <summary>
        /// Any call expression.
        /// </summary>
        public sealed partial record class Call(
            Expr Called,
            Enclosed<PunctuatedList<Expr>> Args) : Expr;

        /// <summary>
        /// Any index expression.
        /// </summary>
        public sealed partial record class Index(
            Expr Called,
            Enclosed<PunctuatedList<Expr>> Args) : Expr;

        /// <summary>
        /// A name reference expression.
        /// </summary>
        public sealed partial record class Name(
            Token Identifier) : Expr;

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed partial record class MemberAccess(
            Expr Object,
            Token DotToken,
            Token MemberName) : Expr;

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed partial record class Unary(
            Token Operator,
            Expr Operand) : Expr;

        /// <summary>
        /// A binary expression, including assignment and compound assignment.
        /// </summary>
        public sealed partial record class Binary(
            Expr Left,
            Token Operator,
            Expr Right) : Expr;

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed partial record class Relational(
            Expr Left,
            ImmutableArray<ComparisonElement> Comparisons) : Expr;

        /// <summary>
        /// A grouping expression, enclosing a sub-expression.
        /// </summary>
        public sealed partial record class Grouping(
            Enclosed<Expr> Expression) : Expr;

        /// <summary>
        /// A string expression composing string content and interpolation.
        /// </summary>
        public sealed partial record class String(
            Token OpenQuotes,
            ImmutableArray<StringPart> Parts,
            Token CloseQuotes) : Expr;
    }

    /// <summary>
    /// The else clause of an if-expression.
    /// </summary>
    public sealed partial record class ElseClause(
        Token ElseToken,
        Expr Expression) : ParseTree;

    /// <summary>
    /// The contents of a block.
    /// </summary>
    public sealed partial record class BlockContents(
        ImmutableArray<Stmt> Statements,
        Expr? Value) : ParseTree;

    /// <summary>
    /// A single comparison element in a comparison chain.
    /// </summary>
    public sealed partial record class ComparisonElement(
        Token Operator,
        Expr Right) : ParseTree;

    /// <summary>
    /// Part of a string literal/expression.
    /// </summary>
    public abstract partial record class StringPart : ParseTree
    {
        /// <summary>
        /// Unexpected tokens in a string.
        /// </summary>
        public sealed partial record class Unexpected(
            ImmutableArray<ParseTree> Elements,
            ImmutableArray<Diagnostic> Diagnostics) : StringPart
        {
            /// <inheritdoc/>
            internal override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
        }

        /// <summary>
        /// Content part of a string literal.
        /// </summary>
        public sealed partial record class Content(
            Token Value,
            int Cutoff,
            ImmutableArray<Diagnostic> Diagnostics) : StringPart
        {
            /// <inheritdoc/>
            internal override ImmutableArray<Diagnostic> Diagnostics { get; } = Diagnostics;
        }

        /// <summary>
        /// An interpolation hole.
        /// </summary>
        public sealed partial record class Interpolation(
            Token OpenToken,
            Expr Expression,
            Token CloseToken) : StringPart;
    }
}

internal abstract partial record class ParseTree
{
    // Plumbing code for width generation
    private static int GetWidth(int x) => 0;
    private static int GetWidth(ImmutableArray<Diagnostic> diags) => 0;
    private static int GetWidth(ParseTree? tree) => tree?.Width ?? 0;
    private static int GetWidth<TElement>(ImmutableArray<TElement> elements)
        where TElement : ParseTree => elements.Sum(e => e.Width);
    private static int GetWidth<TElement>(Enclosed<TElement> element)
        where TElement : ParseTree =>
        element.OpenToken.Width + element.Value.Width + element.CloseToken.Width;
    private static int GetWidth<TElement>(Enclosed<PunctuatedList<TElement>> element)
        where TElement : ParseTree =>
        element.OpenToken.Width + GetWidth(element.Value) + element.CloseToken.Width;
    private static int GetWidth<TElement>(PunctuatedList<TElement> elements)
        where TElement : ParseTree => elements.Elements.Sum(GetWidth);
    private static int GetWidth<TElement>(Punctuated<TElement> element)
        where TElement : ParseTree => element.Value.Width + (element.Punctuation?.Width ?? 0);

    // Plumbing code for children
    private static IEnumerable<ParseTree> GetChildren(int x) => Enumerable.Empty<ParseTree>();
    private static IEnumerable<ParseTree> GetChildren(ImmutableArray<Diagnostic> diags) => Enumerable.Empty<ParseTree>();
    private static IEnumerable<ParseTree> GetChildren(ParseTree? tree)
    {
        if (tree is not null) yield return tree;
    }
    private static IEnumerable<ParseTree> GetChildren<TElement>(ImmutableArray<TElement> elements)
        where TElement : ParseTree => elements.Cast<ParseTree>();
    private static IEnumerable<ParseTree> GetChildren<TElement>(Enclosed<TElement> element)
        where TElement : ParseTree
    {
        yield return element.OpenToken;
        yield return element.Value;
        yield return element.CloseToken;
    }
    private static IEnumerable<ParseTree> GetChildren<TElement>(PunctuatedList<TElement> elements)
        where TElement : ParseTree
    {
        foreach (var p in elements.Elements)
        {
            yield return p.Value;
            if (p.Punctuation is not null) yield return p.Punctuation;
        }
    }
    private static IEnumerable<ParseTree> GetChildren<TElement>(Enclosed<PunctuatedList<TElement>> enclosed)
        where TElement : ParseTree
    {
        yield return enclosed.OpenToken;
        foreach (var p in enclosed.Value.Elements)
        {
            yield return p.Value;
            if (p.Punctuation is not null) yield return p.Punctuation;
        }
        yield return enclosed.CloseToken;
    }
}
