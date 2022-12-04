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
            Symbol.Function DeclarationSymbol,
            Expr.Block Body) : Decl
        {
            [Ignore(IgnoreFlags.Transformer)]
            public ImmutableArray<Symbol.Parameter> Params => this.DeclarationSymbol.Params;

            [Ignore(IgnoreFlags.Transformer)]
            public Type ReturnType => this.DeclarationSymbol.ReturnType;
        }

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
            [Ignore(IgnoreFlags.Transformer)]
            public Type Type => this.DeclarationSymbol.Type;
        }
    }

    /// <summary>
    /// An expression.
    /// </summary>
    public abstract record class Expr : Ast
    {
        /// <summary>
        /// The type this expression evaluates to.
        /// </summary>
        [Ignore(IgnoreFlags.Transformer)]
        public abstract Type EvaluationType { get; }

        /// <summary>
        /// An expression representing unitary value.
        /// </summary>
        public record class Unit(ParseTree? ParseTree) : Expr
        {
            /// <summary>
            /// A default unit value without a parse tree.
            /// </summary>
            public static Unit Default { get; } = new(ParseTree: null);

            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A block expression.
        /// </summary>
        public record class Block(
            ParseTree? ParseTree,
            ImmutableArray<Stmt> Statements,
            Expr Value) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => this.Value.EvaluationType;
        }

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed record class Literal(
            ParseTree? ParseTree,
            object? Value,
            Type Type) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType { get; } = Type;
        }

        /// <summary>
        /// An if-expression with an option elSse clause.
        /// </summary>
        public sealed record class If(
            ParseTree? ParseTree,
            Expr Condition,
            Expr Then,
            Expr Else) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => this.Then.EvaluationType;
        }

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed record class While(
            ParseTree? ParseTree,
            Expr Condition,
            Expr Expression) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            ParseTree? ParseTree,
            Symbol Target) : Expr
        {
            // NOTE: Eventually this should be the bottom type
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed record class Return(
            ParseTree? ParseTree,
            Expr Expression) : Expr
        {
            // NOTE: Eventually this should be the bottom type
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// Any call expression.
        /// </summary>
        public sealed record class Call(
            ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => ((Type.Function)this.Called.EvaluationType).Return;
        }

        /// <summary>
        /// Any index expression.
        /// </summary>
        public sealed record class Index(
            ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => throw new NotImplementedException();
        }

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed record class MemberAccess(
            ParseTree? ParseTree,
            Expr Object,
            Symbol Member) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => throw new NotImplementedException();
        }

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed record class Unary(
            ParseTree? ParseTree,
            Symbol.IOperator Operator,
            Expr Operand) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => this.Operator.ReturnType;
        }

        /// <summary>
        /// A binary expression.
        /// </summary>
        public sealed record class Binary(
            ParseTree? ParseTree,
            Expr Left,
            Symbol.IOperator Operator,
            Expr Right) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => this.Operator.ReturnType;
        }

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed record class Relational(
            ParseTree? ParseTree,
            Expr Left,
            ImmutableArray<ComparisonElement> Comparisons) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => throw new NotImplementedException();
        }

        /// <summary>
        /// An assignment expression, including compound assignment.
        /// </summary>
        public sealed record class Assign(
            ParseTree? ParseTree,
            Expr Target,
            Symbol.IOperator? CompoundOperator,
            Expr Value) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => this.Target.EvaluationType;
        }

        /// <summary>
        /// A short-cutting conjunction expression.
        /// </summary>
        public sealed record class And(
            ParseTree? ParseTree,
            Expr Left,
            Expr Right) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => Type.Bool;
        }

        /// <summary>
        /// A short-cutting disjunction expression.
        /// </summary>
        public sealed record class Or(
            ParseTree? ParseTree,
            Expr Left,
            Expr Right) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => Type.Bool;
        }

        /// <summary>
        /// A string expression composing string content and interpolation.
        /// </summary>
        public sealed record class String(
            ParseTree? ParseTree,
            ImmutableArray<StringPart> Parts) : Expr
        {
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => Type.String;
        }

        /// <summary>
        /// A name reference expression.
        /// </summary>
        public sealed record class Reference(
            ParseTree? ParseTree,
            Symbol Symbol) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.Transformer)]
            public override Type EvaluationType => this.Symbol switch
            {
                // TODO: Maybe just have an ITyped symbol?
                Symbol.Function f => f.Type,
                Symbol.Intrinsic i => i.Type,
                _ => throw new NotImplementedException(),
            };
        }
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
            string Value,
            int Cutoff) : StringPart;

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
        Symbol.IOperator Operator,
        Expr Right) : Ast;

    /// <summary>
    /// A statement in a block.
    /// </summary>
    public abstract record class Stmt : Ast
    {
        /// <summary>
        /// Represents an empty statement.
        /// </summary>
        public sealed record class NoOp(
            ParseTree? ParseTree) : Stmt
        {
            /// <summary>
            /// A default instance to use.
            /// </summary>
            public static NoOp Default { get; } = new(ParseTree: null);
        }

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
