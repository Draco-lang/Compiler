using System.Diagnostics.CodeAnalysis;
using Draco.Compiler.Utilities;

namespace Draco.Compiler.Syntax;

internal abstract record class ParseTree
{
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
            Block Block) : FuncBody;

        public sealed record class InlineBody(
            IToken EqualsToken,
            Expr Expression) : FuncBody;
    }

    public abstract record class TypeName : ParseTree
    {
        // This is the only kind of type name for now
        public sealed record class Simple(
            IToken Identifier) : TypeName;
    }

    public sealed record class TypeSpecifier(
        IToken ColonToken,
        TypeName Type) : ParseTree;

    public sealed record class Block(
        ValueArray<Stmt> Statements,
        Expr? Expression) : ParseTree;

    public abstract record class Stmt : ParseTree
    {
        public sealed record class DeclStmt(
            Decl Declaration) : Stmt;

        public sealed record class ExprStmt(
            Expr Expression,
            IToken Semicolon) : Stmt;
    }

    public abstract record class Expr : ParseTree
    {
        public sealed record class BlockExpr(
            Block Expression) : Expr;

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
    }
}
