using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

/// <summary>
/// An immutable structure representing a parsed source text with information about concrete syntax.
/// </summary>
internal abstract record class ParseTree
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
            (Token AssignToken, Expr Expression)? Initializer) : Decl;
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
    public abstract record class FuncBody
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
