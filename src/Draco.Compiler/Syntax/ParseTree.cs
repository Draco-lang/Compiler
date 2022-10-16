using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Draco.Compiler.Diagnostics;
using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

/// <summary>
/// An immutable structure representing a parsed source text with information about concrete syntax.
/// </summary>
internal abstract partial record class ParseTree
{
    /// <summary>
    /// The diagnostics attached to this tree node.
    /// </summary>
    public virtual ValueArray<Diagnostic> Diagnostics => ValueArray<Diagnostic>.Empty;
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
    public sealed record class CompilationUnit(
        ValueArray<Decl> Declarations) : ParseTree;

    /// <summary>
    /// A declaration, either top-level or as a statement.
    /// </summary>
    public abstract record class Decl : ParseTree
    {
        /// <summary>
        /// Unexpected input in declaration context.
        /// </summary>
        public sealed record class Unexpected : Decl
        {
            /// <summary>
            /// The sequence of tokens that were unexpected.
            /// </summary>
            public ValueArray<Token> Tokens { get; }

            /// <inheritdoc/>
            public override ValueArray<Diagnostic> Diagnostics { get; }

            public Unexpected(ValueArray<Token> tokens, ValueArray<Diagnostic> diagnostics)
            {
                this.Tokens = tokens;
                this.Diagnostics = diagnostics;
            }
        }

        /// <summary>
        /// A function declaration.
        /// </summary>
        public sealed record class Func(
            Token FuncKeyword,
            Token Identifier,
            Enclosed<PunctuatedList<FuncParam>> Params,
            TypeSpecifier? ReturnType,
            FuncBody Body) : Decl;

        /// <summary>
        /// A label declaration.
        /// </summary>
        public sealed record class Label(
            Token Identifier,
            Token ColonToken) : Decl;

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed record class Variable(
            Token Keyword, // Either var or val
            Token Identifier,
            TypeSpecifier? Type,
            (Token AssignToken, Expr Expression)? Initializer,
            Token Semicolon) : Decl;
    }

    /// <summary>
    /// A function parameter.
    /// </summary>
    public sealed record class FuncParam(
        Token Identifier,
        TypeSpecifier Type) : ParseTree;

    /// <summary>
    /// A function body, either a block or in-line.
    /// </summary>
    public abstract record class FuncBody : ParseTree
    {
        /// <summary>
        /// A block function body.
        /// </summary>
        public sealed record class BlockBody(
            Expr.Block Block) : FuncBody;

        /// <summary>
        /// An in-line function body.
        /// </summary>
        public sealed record class InlineBody(
            Token AssignToken,
            Expr Expression,
            Token Semicolon) : FuncBody;
    }

    /// <summary>
    /// A type expression, i.e. a reference to a type.
    /// </summary>
    public abstract record class TypeExpr : ParseTree
    {
        // This is the only kind of type expression for now
        public sealed record class Name(
            Token Identifier) : TypeExpr;
    }

    /// <summary>
    /// A type specifier for functions, variables, expressions, etc.
    /// </summary>
    public sealed record class TypeSpecifier(
        Token ColonToken,
        TypeExpr Type) : ParseTree;

    /// <summary>
    /// A statement in a block.
    /// </summary>
    public abstract record class Stmt : ParseTree
    {
        /// <summary>
        /// A declaration statement.
        /// </summary>
        public new sealed record class Decl(
            ParseTree.Decl Declaration) : Stmt;

        /// <summary>
        /// An expression statement.
        /// </summary>
        public new sealed record class Expr(
            ParseTree.Expr Expression,
            Token? Semicolon) : Stmt;
    }

    /// <summary>
    /// An expression.
    /// </summary>
    public abstract record class Expr : ParseTree
    {
        /// <summary>
        /// Unexpected input in expression context.
        /// </summary>
        public sealed record class Unexpected : Expr
        {
            /// <summary>
            /// The sequence of tokens that were unexpected.
            /// </summary>
            public ValueArray<Token> Tokens { get; }

            /// <inheritdoc/>
            public override ValueArray<Diagnostic> Diagnostics { get; }

            public Unexpected(ValueArray<Token> tokens, ValueArray<Diagnostic> diagnostics)
            {
                this.Tokens = tokens;
                this.Diagnostics = diagnostics;
            }
        }

        /// <summary>
        /// An expression that results in unit type and only executes a statement.
        /// </summary>
        public sealed record class UnitStmt(
            Stmt Statement) : Expr;

        /// <summary>
        /// A block of statements and an optional value.
        /// </summary>
        public sealed record class Block(
            Enclosed<(ValueArray<Stmt> Statements, Expr? Value)> Enclosed) : Expr;

        /// <summary>
        /// An if-expression with an option else clause.
        /// </summary>
        public sealed record class If(
            Token IfKeyword,
            Enclosed<Expr> Condition,
            Expr Then,
            (Token ElseToken, Expr Expression)? Else) : Expr;

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed record class While(
            Token WhileKeyword,
            Enclosed<Expr> Condition,
            Expr Expression) : Expr;

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            Token GotoKeyword,
            Token Identifier) : Expr;

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed record class Return(
            Token ReturnKeyword,
            Expr? Expression) : Expr;

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed record class Literal(
            Token Value) : Expr;

        /// <summary>
        /// Any call-like expression.
        /// </summary>
        public sealed record class Call(
            Expr Called,
            Enclosed<PunctuatedList<Expr>> Args) : Expr;

        /// <summary>
        /// A name reference expression.
        /// </summary>
        public sealed record class Name(
            Token Identifier) : Expr;

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed record class MemberAccess(
            Expr Object,
            Token DotToken,
            Token MemberName) : Expr;

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed record class Unary(
            Token Operator,
            Expr Operand) : Expr;

        /// <summary>
        /// A binary expression, including assignment and compound assignment.
        /// </summary>
        public sealed record class Binary(
            Expr Left,
            Token Operator,
            Expr Right) : Expr;

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed record class Relational(
            Expr Left,
            ValueArray<(Token Operator, Expr Right)> Comparisons) : Expr;

        /// <summary>
        /// A grouping expression, enclosing a sub-expression.
        /// </summary>
        public sealed record class Grouping(
            Enclosed<Expr> Expression) : Expr;
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
