using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Api.Syntax;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// An immutable structure representing semantic information about source code.
/// </summary>
internal abstract record class Ast(ParseTree? ParseTree)
{
    /// <summary>
    /// A declaration, either top-level or as a statement.
    /// </summary>
    public abstract record class Decl(ParseTree? ParseTree) : Ast(ParseTree)
    {
        /// <summary>
        /// A function declaration.
        /// </summary>
        public sealed record class Func(
            ParseTree? ParseTree,
            Symbol DeclarationSymbol,
            ImmutableArray<Symbol> Params,
            Type ReturnType,
            FuncBody Body) : Decl(ParseTree);

        /// <summary>
        /// A label declaration.
        /// </summary>
        public sealed record class Label(
            ParseTree? ParseTree,
            Symbol LabelSymbol) : Decl(ParseTree);

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed record class Variable(
            ParseTree? ParseTree,
            Symbol DeclarationSymbol,
            Symbol Type,
            Expr Value) : Decl(ParseTree);
    }

    /// <summary>
    /// A function body.
    /// </summary>
    public record class FuncBody(ParseTree? ParseTree) : Ast(ParseTree)
    {
        /// <summary>
        /// A block function body.
        /// </summary>
        public sealed record class BlockBody(
            ParseTree? ParseTree,
            Expr.Block Block) : FuncBody(ParseTree);

        /// <summary>
        /// An in-line function body.
        /// </summary>
        public sealed record class InlineBody(
            ParseTree? ParseTree,
            Expr Expression) : FuncBody(ParseTree);
    }

    /// <summary>
    /// An expression.
    /// </summary>
    public abstract record class Expr(ParseTree? ParseTree) : Ast(ParseTree)
    {
        /// <summary>
        /// An expression representing unitary value.
        /// </summary>
        public record class Unit(ParseTree? ParseTree) : Expr(ParseTree);

        /// <summary>
        /// A block expression.
        /// </summary>
        public record class Block(
            ParseTree? ParseTree,
            ImmutableArray<Stmt> Statements,
            Expr Value) : Expr(ParseTree);

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed record class Literal(
            ParseTree? ParseTree,
            object Value,
            Symbol Type) : Expr(ParseTree);

        /// <summary>
        /// An if-expression with an option else clause.
        /// </summary>
        public sealed record class If(
            ParseTree? ParseTree,
            Expr Condition,
            Expr Then,
            Expr Else) : Expr(ParseTree);

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed record class While(
            ParseTree? ParseTree,
            Expr Condition,
            Expr Expression) : Expr(ParseTree);

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            ParseTree? ParseTree,
            Symbol Target) : Expr(ParseTree);

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed record class Return(
            ParseTree? ParseTree,
            Expr Expression) : Expr(ParseTree);

        /// <summary>
        /// Any call expression.
        /// </summary>
        public sealed record class Call(
            ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr(ParseTree);

        /// <summary>
        /// Any index expression.
        /// </summary>
        public sealed record class Index(
            ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr(ParseTree);

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed record class MemberAccess(
            ParseTree? ParseTree,
            Expr Object,
            Symbol Member) : Expr(ParseTree);

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed record class Unary(
            ParseTree? ParseTree,
            Symbol Operator,
            Expr Operand) : Expr(ParseTree);

        /// <summary>
        /// A binary expression, including assignment and compound assignment.
        /// </summary>
        public sealed record class Binary(
            ParseTree? ParseTree,
            Expr Left,
            Symbol Operator,
            Expr Right) : Expr(ParseTree);

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed record class Relational(
            ParseTree? ParseTree,
            Expr Left,
            ImmutableArray<ComparisonElement> Comparisons) : Expr(ParseTree);

        /// <summary>
        /// A string expression composing string content and interpolation.
        /// </summary>
        public sealed record class String(
            ParseTree? ParseTree,
            ImmutableArray<StringPart> Parts) : Expr(ParseTree);
    }

    /// <summary>
    /// Part of a string literal/expression.
    /// </summary>
    public abstract record class StringPart(ParseTree? ParseTree) : Ast(ParseTree)
    {
        /// <summary>
        /// Content part of a string literal.
        /// </summary>
        public sealed record class Content(
            ParseTree? ParseTree,
            string Value) : StringPart(ParseTree);

        /// <summary>
        /// An interpolation hole.
        /// </summary>
        public sealed record class Interpolation(
            ParseTree? ParseTree,
            Expr Expression) : StringPart(ParseTree);
    }

    /// <summary>
    /// A single comparison element in a comparison chain.
    /// </summary>
    public record class ComparisonElement(
        ParseTree? ParseTree,
        Symbol Operator,
        Expr Right) : Ast(ParseTree);

    /// <summary>
    /// A statement in a block.
    /// </summary>
    public abstract record class Stmt(ParseTree? ParseTree) : Ast(ParseTree)
    {
        /// <summary>
        /// A declaration statement.
        /// </summary>
        public new sealed record class Decl(
            ParseTree? ParseTree,
            Ast.Decl Declaration) : Stmt(ParseTree);

        /// <summary>
        /// An expression statement.
        /// </summary>
        public new sealed record class Expr(
            ParseTree? ParseTree,
            ParseTree.Expr Expression) : Stmt(ParseTree);
    }
}
