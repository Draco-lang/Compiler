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
    public sealed record class Enclosed<T>(
        IToken OpenToken,
        T Value,
        IToken CloseToken) : ParseTree;

    /// <summary>
    /// A list of nodes with a token separating each element.
    /// </summary>
    public sealed record class PunctuatedList<T>(
        ValueArray<(
            T Value,
            IToken? Punctuation
        )> Elements) : ParseTree;

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
            IToken FuncKeyword,
            IToken Identifier,
            Enclosed<PunctuatedList<FuncParam>> Params,
            TypeSpecifier? Type,
            FuncBody Body) : Decl;

        /// <summary>
        /// A label declaration.
        /// </summary>
        public sealed record class Label(
            IToken Identifier,
            IToken ColonToken) : Decl;

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed record class Variable(
            IToken Keyword, // Either var or val
            IToken Identifier,
            TypeSpecifier? Type,
            (   IToken EqualsToken,
                Expr Expression
            )? Initializer) : Decl;
    }

    /// <summary>
    /// A function parameter.
    /// </summary>
    public sealed record class FuncParam(
        IToken Identifier,
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
            IToken EqualsToken,
            Expr Expression) : FuncBody;
    }

    /// <summary>
    /// A type expression, i.e. a reference to a type.
    /// </summary>
    public abstract record class TypeExpr : ParseTree
    {
        // This is the only kind of type expression for now
        public sealed record class Name(
            IToken Identifier) : TypeExpr;
    }

    /// <summary>
    /// A type specifier for functions, variables, expressions, etc.
    /// </summary>
    public sealed record class TypeSpecifier(
        IToken ColonToken,
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
            IToken Semicolon) : Stmt;
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
            Enclosed<(
                ValueArray<Stmt> Statements,
                Expr? Value
            )> Enclosed) : Expr;

        /// <summary>
        /// An if-expression with an option else clause.
        /// </summary>
        public sealed record class If(
            IToken IfKeyword,
            Enclosed<Expr> Condition,
            Expr Expression,
            (   IToken ElseToken,
                Expr Expression
            )? Else) : Expr;

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed record class While(
            IToken Token,
            Enclosed<Expr> Condition,
            Expr Expression) : Expr;

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            IToken GotoKeyword,
            IToken Identifier) : Expr;

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed record class Return(
            IToken ReturnKeyword,
            Expr? Expression) : Expr;

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed record class Literal(
            IToken Value) : Expr;

        /// <summary>
        /// A function call expression.
        /// </summary>
        public sealed record class FuncCall(
            Expr Expression,
            Enclosed<PunctuatedList<Expr>> Args) : Expr;

        /// <summary>
        /// An indexer expression.
        /// </summary>
        public sealed record class Index(
            Expr Expression,
            Enclosed<Expr> IndexExpression) : Expr;

        /// <summary>
        /// A variable expression, or more correctly an identifier reference expression.
        /// </summary>
        public sealed record class Variable(
            IToken Identifier) : Expr;

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed record class MemberAccess(
            Expr Expression,
            IToken PeriodToken,
            IToken MemberName) : Expr;

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed record class Unary(
            IToken Operator,
            Expr Operand) : Expr;

        /// <summary>
        /// A binary expression, including assignment and compound assignment.
        /// </summary>
        public sealed record class Binary(
            Expr Left,
            IToken Operator,
            Expr Right) : Expr;

        /// <summary>
        /// A grouping expression, enclosing a sub-expression.
        /// </summary>
        public sealed record class Grouping(
            Enclosed<Expr> Expression);
    }
}
