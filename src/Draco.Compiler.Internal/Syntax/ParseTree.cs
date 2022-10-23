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

    /// <summary>
    /// The diagnostics attached to this tree node.
    /// </summary>
    public virtual ValueArray<Diagnostic> Diagnostics => ValueArray<Diagnostic>.Empty;

    // Plumbing code for width generation
    private static int GetWidth(ValueArray<Diagnostic> diags) => 0;
    private static int GetWidth(ParseTree? tree) => tree?.Width ?? 0;
    private static int GetWidth<TElement>(ValueArray<TElement> elements)
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
        ValueArray<Punctuated<T>> Elements);

    /// <summary>
    /// A compilation unit, the top-most node in the parse tree.
    /// </summary>
    public sealed partial record class CompilationUnit(
        ValueArray<Decl> Declarations) : ParseTree;

    /// <summary>
    /// A declaration, either top-level or as a statement.
    /// </summary>
    public abstract partial record class Decl : ParseTree
    {
        /// <summary>
        /// Unexpected input in declaration context.
        /// </summary>
        public sealed partial record class Unexpected(
            ValueArray<Token> Tokens,
            ValueArray<Diagnostic> Diagnostics) : Decl
        {
            /// <inheritdoc/>
            public override ValueArray<Diagnostic> Diagnostics { get; } = Diagnostics;
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
            ValueArray<Token> Tokens,
            ValueArray<Diagnostic> Diagnostics) : FuncBody
        {
            /// <inheritdoc/>
            public override ValueArray<Diagnostic> Diagnostics { get; } = Diagnostics;
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
            ValueArray<Token> Tokens,
            ValueArray<Diagnostic> Diagnostics) : Expr
        {
            /// <inheritdoc/>
            public override ValueArray<Diagnostic> Diagnostics { get; } = Diagnostics;
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
            Token Identifier) : Expr;

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
        /// Any call-like expression.
        /// </summary>
        public sealed partial record class Call(
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
            ValueArray<ComparisonElement> Comparisons) : Expr;

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
            ValueArray<StringPart> Parts,
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
        ValueArray<Stmt> Statements,
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
        /// Content part of a string literal.
        /// </summary>
        public sealed partial record class Content(
            Token Token,
            ValueArray<Diagnostic> Diagnostics) : StringPart
        {
            /// <inheritdoc/>
            public override ValueArray<Diagnostic> Diagnostics { get; } = Diagnostics;
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

// Pretty printer
internal partial record class ParseTree
{
    /// <summary>
    /// Prints this <see cref="ParseTree"/> in a debuggable form.
    /// </summary>
    /// <returns>The pretty-printed <see cref="ParseTree"/> text.</returns>
    public string PrettyPrint()
    {
        var builder = new StringBuilder();
        var printer = new PrettyPrinter(builder);
        printer.Print(this, 0);
        return builder.ToString();
    }

    private sealed class PrettyPrinter
    {
        public StringBuilder Builder { get; init; }
        public string Indentation { get; init; } = "  ";

        public PrettyPrinter(StringBuilder builder)
        {
            this.Builder = builder;
        }

        public Unit Print(object? obj, int depth) => obj switch
        {
            Token token => this.PrintToken(token),
            ParseTree parseTree => this.PrintSubtree(parseTree.GetType().Name, parseTree, depth),
            IEnumerable<object> collection => this.PrintCollection(collection, depth),
            ITuple tuple => this.PrintTuple(tuple, depth),
            Diagnostic diagnostic => this.PrintText(DiagnosticToString(diagnostic)),
            string str => this.PrintText(str),
            null => this.PrintText("null"),
            object o when o.GetType() is var type
                       && type.IsGenericType
                       && type.GetGenericTypeDefinition() == typeof(Enclosed<>) =>
                this.PrintSubtree("Enclosed", o, depth),
            object o when o.GetType() is var type
                       && type.IsGenericType
                       && type.GetGenericTypeDefinition() == typeof(PunctuatedList<>) =>
                this.PrintCollection(
                    (IEnumerable)type.GetProperty(nameof(PunctuatedList<int>.Elements))!.GetValue(o)!,
                    depth),
            object o when o.GetType() is var type
                       && type.IsGenericType
                       && type.GetGenericTypeDefinition() == typeof(Punctuated<>) =>
                this.PrintSubtree("Punctuated", o, depth),
            IEnumerable collection => this.PrintCollection(collection, depth),
            _ => throw new System.NotImplementedException(),
        };

        private Unit PrintSubtree(string name, object tree, int depth)
        {
            this.Builder.Append(name).Append(' ');
            return this.PrintRecursive(
                tree.GetType().GetProperties().Select(p => ((string?)p.Name, p.GetValue(tree))),
                depth,
                open: '{',
                close: '}');
        }

        private Unit PrintCollection(IEnumerable collection, int depth) =>
            this.PrintRecursive(collection.Cast<object?>().Select(v => ((string?)null, v)), depth, open: '[', close: ']');

        private Unit PrintTuple(ITuple tuple, int depth) => this.PrintRecursive(
            Enumerable.Range(0, tuple.Length).Select(i => ((string?)$"Item{i + 1}", tuple[i])),
            depth, open: '(', close: ')');

        private Unit PrintToken(Token token)
        {
            this.Builder.Append('\'').Append(token.Text).Append('\'');
            var valueText = token.ValueText;
            if (valueText is not null && valueText != token.Text)
            {
                this.Builder.Append(" (value=").Append(valueText).Append(')');
            }
            if (token.Diagnostics.Count > 0)
            {
                this.Builder.Append(" [");
                this.Builder.AppendJoin(", ", token.Diagnostics.Select(DiagnosticToString));
                this.Builder.Append(']');
            }
            return default;
        }

        private Unit PrintText(string text)
        {
            this.Builder.Append(text);
            return default;
        }

        private Unit PrintRecursive(IEnumerable<(string? Key, object? Value)> values, int depth, char open, char close)
        {
            if (!values.Any())
            {
                this.Builder.Append(open).Append(close);
                return default;
            }

            this.Builder.AppendLine(open.ToString());
            foreach (var (key, item) in values)
            {
                this.Indent(depth + 1);
                if (key is not null) this.Builder.Append(key).Append(": ");
                this.Print(item, depth + 1);
                this.Builder.AppendLine(", ");
            }
            this.Indent(depth);
            this.Builder.Append(close);

            return default;
        }

        private void Indent(int depth)
        {
            for (var i = 0; i < depth; ++i) this.Builder.Append(this.Indentation);
        }

        private static string DiagnosticToString(Diagnostic diagnostic) =>
            string.Format(diagnostic.Format, diagnostic.FormatArgs);
    }
}
