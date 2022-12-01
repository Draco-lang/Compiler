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
using Draco.RedGreenTree.Attributes;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// An immutable structure representing semantic information about source code.
/// </summary>
internal abstract record class Ast
{
    public abstract ParseTree? ParseTree { get; init; }

    /// <summary>
    /// An entire compilation unit.
    /// </summary>
    public sealed record class CompilationUnit(
        ParseTree? ParseTree,
        ImmutableArray<Decl> Declarations) : Ast;

    /// <summary>
    /// A declaration, either top-level or as a statement.
    /// </summary>
    public abstract record class Decl : Ast
    {
        /// <summary>
        /// A function declaration.
        /// </summary>
        public sealed record class Func(
            ParseTree? ParseTree,
            Symbol DeclarationSymbol,
            ImmutableArray<Symbol> Params,
            Type ReturnType,
            Expr.Block Body) : Decl;

        /// <summary>
        /// A label declaration.
        /// </summary>
        public sealed record class Label(
            ParseTree? ParseTree,
            Symbol LabelSymbol) : Decl;

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed record class Variable(
            ParseTree? ParseTree,
            Symbol.IVariable DeclarationSymbol,
            Expr? Value) : Decl
        {
            [Ignore(IgnoreFlags.Transformer)] public Type Type => this.DeclarationSymbol.Type;
        }
    }

    /// <summary>
    /// An expression.
    /// </summary>
    public abstract record class Expr : Ast
    {
        /// <summary>
        /// An expression representing unitary value.
        /// </summary>
        public record class Unit(ParseTree? ParseTree) : Expr
        {
            /// <summary>
            /// A default unit value without a parse tree.
            /// </summary>
            public static Unit Default { get; } = new(ParseTree: null);
        }

        /// <summary>
        /// A block expression.
        /// </summary>
        public record class Block(
            ParseTree? ParseTree,
            ImmutableArray<Stmt> Statements,
            Expr Value) : Expr;

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed record class Literal(
            ParseTree? ParseTree,
            object Value,
            Symbol Type) : Expr;

        /// <summary>
        /// An if-expression with an option else clause.
        /// </summary>
        public sealed record class If(
            ParseTree? ParseTree,
            Expr Condition,
            Expr Then,
            Expr Else) : Expr;

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed record class While(
            ParseTree? ParseTree,
            Expr Condition,
            Expr Expression) : Expr;

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            ParseTree? ParseTree,
            Symbol Target) : Expr;

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed record class Return(
            ParseTree? ParseTree,
            Expr Expression) : Expr;

        /// <summary>
        /// Any call expression.
        /// </summary>
        public sealed record class Call(
            ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr;

        /// <summary>
        /// Any index expression.
        /// </summary>
        public sealed record class Index(
            ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr;

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed record class MemberAccess(
            ParseTree? ParseTree,
            Expr Object,
            Symbol Member) : Expr;

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed record class Unary(
            ParseTree? ParseTree,
            Symbol Operator,
            Expr Operand) : Expr;

        /// <summary>
        /// A binary expression, including assignment and compound assignment.
        /// </summary>
        public sealed record class Binary(
            ParseTree? ParseTree,
            Expr Left,
            Symbol Operator,
            Expr Right) : Expr;

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed record class Relational(
            ParseTree? ParseTree,
            Expr Left,
            ImmutableArray<ComparisonElement> Comparisons) : Expr;

        /// <summary>
        /// A string expression composing string content and interpolation.
        /// </summary>
        public sealed record class String(
            ParseTree? ParseTree,
            ImmutableArray<StringPart> Parts) : Expr;

        /// <summary>
        /// A name reference expression.
        /// </summary>
        public sealed record class Reference(
            ParseTree? ParseTree,
            Symbol Symbol) : Expr;
    }

    /// <summary>
    /// Part of a string literal/expression.
    /// </summary>
    public abstract record class StringPart : Ast
    {
        /// <summary>
        /// Content part of a string literal.
        /// </summary>
        public sealed record class Content(
            ParseTree? ParseTree,
            string Value) : StringPart;

        /// <summary>
        /// An interpolation hole.
        /// </summary>
        public sealed record class Interpolation(
            ParseTree? ParseTree,
            Expr Expression) : StringPart;
    }

    /// <summary>
    /// A single comparison element in a comparison chain.
    /// </summary>
    public record class ComparisonElement(
        ParseTree? ParseTree,
        Symbol Operator,
        Expr Right) : Ast;

    /// <summary>
    /// A statement in a block.
    /// </summary>
    public abstract record class Stmt : Ast
    {
        /// <summary>
        /// A declaration statement.
        /// </summary>
        public new sealed record class Decl(
            ParseTree? ParseTree,
            Ast.Decl Declaration) : Stmt;

        /// <summary>
        /// An expression statement.
        /// </summary>
        public new sealed record class Expr(
            ParseTree? ParseTree,
            Ast.Expr Expression) : Stmt;
    }
}
