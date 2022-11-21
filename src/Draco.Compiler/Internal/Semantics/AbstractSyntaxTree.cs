using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Internal.Semantics;

internal abstract record class AbstractSyntaxTree
{
    public abstract record class Decl : AbstractSyntaxTree
    {
        /// <summary>
        /// A function declaration.
        /// </summary>
        public sealed record class Func(
            Symbol DeclarationSymbol,
            ImmutableArray<Symbol> Params,
            Symbol ReturnType,
            FuncBody Body) : Decl;

        /// <summary>
        /// A label declaration.
        /// </summary>
        public sealed record class Label(
            Symbol LabelSymbol) : Decl;

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed record class Variable(
            Symbol DeclarationSymbol,
            Symbol Type,
            Expr Value) : Decl;
    }

    /// <summary>
    /// A function body
    /// </summary>
    public record class FuncBody : AbstractSyntaxTree
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
            Expr Expression) : FuncBody;
    }

    public abstract record class Expr : AbstractSyntaxTree
    {
        public record class Unit() : Expr;
        /// <summary>
        /// A block expression
        /// </summary>
        public record class Block(
            ImmutableArray<Stmt> Statements,
            Expr Value) : Expr;

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed record class Literal(
            object Value,
            Symbol Type) : Expr;

        /// <summary>
        /// An if-expression with an option else clause.
        /// </summary>
        public sealed record class If(
            Expr Condition,
            Expr Then,
            Expr Else) : Expr;

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed record class While(
            Expr Condition,
            Expr Expression) : Expr;

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            Symbol Target) : Expr;

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed record class Return(
            Expr Expression) : Expr;

        /// <summary>
        /// Any call expression.
        /// </summary>
        public sealed record class Call(
            Expr Called,
            ImmutableArray<Expr> Args) : Expr;

        /// <summary>
        /// Any index expression.
        /// </summary>
        public sealed record class Index(
            Expr Called,
            ImmutableArray<Expr> Args) : Expr;

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed record class MemberAccess(
            Expr Object,
            Symbol MemberName) : Expr;

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed record class Unary(
            Symbol Operator,
            Expr Operand) : Expr;

        /// <summary>
        /// A binary expression, including assignment and compound assignment.
        /// </summary>
        public sealed record class Binary(
            Expr Left,
            Symbol Operator,
            Expr Right) : Expr;

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed record class Relational(
            Expr Left,
            ImmutableArray<ComparisonElement> Comparisons) : Expr;

        /// <summary>
        /// A string expression composing string content and interpolation.
        /// </summary>
        public sealed record class String(
            ImmutableArray<StringPart> Parts) : Expr;
    }

    /// <summary>
    /// Part of a string literal/expression.
    /// </summary>
    public abstract record class StringPart : AbstractSyntaxTree
    {
        /// <summary>
        /// Content part of a string literal.
        /// </summary>
        public sealed record class Content(
            Symbol Value) : StringPart;

        /// <summary>
        /// An interpolation hole.
        /// </summary>
        public sealed record class Interpolation(
            Expr Expression) : StringPart;
    }

    /// <summary>
    /// A single comparison element in a comparison chain.
    /// </summary>
    public record class ComparisonElement(
        Symbol Operator,
        Expr Right) : AbstractSyntaxTree;

    public abstract record class Stmt : AbstractSyntaxTree
    {
        /// <summary>
        /// A declaration statement.
        /// </summary>
        public new sealed record class Decl(
            AbstractSyntaxTree.Decl Declaration) : Stmt;

        /// <summary>
        /// An expression statement.
        /// </summary>
        public new sealed record class Expr(
            ParseTree.Expr Expression) : Stmt;
    }
}
