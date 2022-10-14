using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

/// <summary>
/// An immutable structure representing a parsed source text with information about concrete syntax.
/// </summary>
internal abstract record class ParseTree
{
    public sealed record class CompilationUnit(
        ValueArray<Decl> Declarations) : ParseTree;

    public abstract record class Decl : ParseTree
    {
        public sealed record class Func(
            IToken FuncKeyword,
            IToken Identifier,
            IToken OpenParenToken,
            ValueArray<(
                (   IToken Identifier,
                    TypeSpecifier Type
                ) Param,
                IToken CommaToken
            )> Params,
            IToken CloseParenToken,
            TypeSpecifier? Type,
            FuncBody Body) : Decl;

        public sealed record class Label(
            IToken Identifier,
            IToken ColonToken) : Decl;

        public sealed record class Variable(
            IToken Keyword, // Either var or val
            IToken Identifier,
            TypeSpecifier? Type,
            (   IToken EqualsToken,
                Expr Expression
            )? Initializer) : Decl;
    }

    public abstract record class FuncBody
    {
        public sealed record class BlockBody(
            Expr.Block Block) : FuncBody;

        public sealed record class InlineBody(
            IToken EqualsToken,
            Expr Expression) : FuncBody;
    }

    public abstract record class TypeExpr : ParseTree
    {
        // This is the only kind of type expression for now
        public sealed record class Name(
            IToken Identifier) : TypeExpr;
    }

    public sealed record class TypeSpecifier(
        IToken ColonToken,
        TypeExpr Type) : ParseTree;

    public abstract record class Stmt : ParseTree
    {
        public new sealed record class Decl(
            ParseTree.Decl Declaration) : Stmt;

        public new sealed record class Expr(
            ParseTree.Expr Expression,
            IToken Semicolon) : Stmt;
    }

    public abstract record class Expr : ParseTree
    {
        public sealed record class Block(
            ValueArray<Stmt> Statements,
            Expr? Value) : Expr;

        public sealed record class If(
            IToken IfKeyword,
            IToken OpenParenToken,
            Expr Condition,
            IToken CloseParenToken,
            Expr Expression,
            (   IToken ElseToken,
                Expr Expression
            )? Else) : Expr;

        public sealed record class While(
            IToken Token,
            IToken OpenParenToken,
            Expr Condition,
            IToken CloseParenToken,
            Expr Expression) : Expr;

        public sealed record class Goto(
            IToken GotoKeyword,
            IToken Identifier) : Expr;

        public sealed record class Return(
            IToken ReturnKeyword,
            Expr? Expression) : Expr;

        public sealed record class Literal(
            IToken Value) : Expr;

        public sealed record class FuncCall(
            Expr Expression,
            IToken OpenParenToken,
            ValueArray<(
                Expr Expression,
                IToken CommaToken
            )> Args,
            IToken CloseParenToken) : Expr;

        public sealed record class Index(
            Expr Expression,
            IToken OpenBracketToken,
            Expr IndexExpression,
            IToken CloseBracketToken) : Expr;

        public sealed record class MemberAccess(
            Expr Expression,
            IToken PeriodToken,
            IToken MemberName) : Expr;

        public sealed record class Unary(
            IToken Operator,
            Expr Operand) : Expr;

        // Binary includes assignment
        public sealed record class Binary(
            Expr Left,
            IToken Operator,
            Expr Right) : Expr;
    }
}
