using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.RedGreenTree.Cli;

internal class Program
{
    internal static void Main(string[] args)
    {
        var source = """
/// <summary>
/// An immutable structure representing a parsed source text with information about concrete syntax.
/// </summary>
internal abstract partial record class ParseTree
{
    /// <summary>
    /// The diagnostics attached to this tree node.
    /// </summary>
    public virtual ValueArray<Diagnostic> Diagnostics => ValueArray<Diagnostic>.Empty;

    public abstract int Width { get; }
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
            (Token AssignToken, Expr Expression)? Initializer,
            Token Semicolon) : Decl;
    }

    /// <summary>
    /// A function parameter.
    /// </summary>
    public sealed partial record class FuncParam(
        Token Identifier,
        TypeSpecifier Type) : ParseTree;

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
            Enclosed<(ValueArray<Stmt> Statements, Expr? Value)> Enclosed) : Expr;

        /// <summary>
        /// An if-expression with an option else clause.
        /// </summary>
        public sealed partial record class If(
            Token IfKeyword,
            Enclosed<Expr> Condition,
            Expr Then,
            (Token ElseToken, Expr Expression)? Else) : Expr;

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
            ValueArray<(Token Operator, Expr Right)> Comparisons) : Expr;

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
    /// Part of a string literal/expression.
    /// </summary>
    public abstract record class StringPart : ParseTree
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

record struct ValueArray<T>;
""";
        var parseTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("MyCompilation", new[] { parseTree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var semanticModel = compilation.GetSemanticModel(parseTree);
        var rootType = semanticModel.GetDeclaredSymbol(parseTree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().First());

        var generatedCode = GreenTreeGenerator.Generate(rootType!);
        Console.WriteLine(generatedCode);
    }
}
