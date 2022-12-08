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
        [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IFunction DeclarationSymbol,
            Expr.Block Body) : Decl
        {
            [Ignore(IgnoreFlags.VisitorVisit | IgnoreFlags.TransformerAll)]
            public ImmutableArray<ISymbol.IParameter> Params => this.DeclarationSymbol.Parameters;

            [Ignore(IgnoreFlags.TransformerAll)]
            public Type ReturnType => this.DeclarationSymbol.ReturnType;
        }

        /// <summary>
        /// A label declaration.
        /// </summary>
        public sealed record class Label(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.ILabel LabelSymbol) : Decl;

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed record class Variable(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IVariable DeclarationSymbol,
            Expr? Value) : Decl
        {
            [Ignore(IgnoreFlags.TransformerAll)]
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
        [Ignore(IgnoreFlags.TransformerAll)]
        public abstract Type EvaluationType { get; }

        /// <summary>
        /// An expression representing unitary value.
        /// </summary>
        public record class Unit(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree) : Expr
        {
            /// <summary>
            /// A default unit value without a parse tree.
            /// </summary>
            public static Unit Default { get; } = new(ParseTree: null);

            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A block expression.
        /// </summary>
        public record class Block(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            ImmutableArray<Stmt> Statements,
            Expr Value) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Value.EvaluationType;
        }

        /// <summary>
        /// A literal expression, i.e. a number, string, boolean value, etc.
        /// </summary>
        public sealed record class Literal(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] object? Value,
            [property: Ignore(IgnoreFlags.TransformerTransform)] Type Type) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType { get; } = Type;
        }

        /// <summary>
        /// An if-expression with an option elSse clause.
        /// </summary>
        public sealed record class If(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Condition,
            Expr Then,
            Expr Else) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Then.EvaluationType;
        }

        /// <summary>
        /// A while-expression.
        /// </summary>
        public sealed record class While(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Condition,
            Expr Expression) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.ILabel Target) : Expr
        {
            // NOTE: Eventually this should be the bottom type
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A return-expression.
        /// </summary>
        public sealed record class Return(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Expression) : Expr
        {
            // NOTE: Eventually this should be the bottom type
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// Any call expression.
        /// </summary>
        public sealed record class Call(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => ((Type.Function)this.Called.EvaluationType).Return;
        }

        /// <summary>
        /// Any index expression.
        /// </summary>
        public sealed record class Index(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Called,
            ImmutableArray<Expr> Args) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => throw new NotImplementedException();
        }

        /// <summary>
        /// A member access expression.
        /// </summary>
        public sealed record class MemberAccess(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Object,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IMember Member) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => throw new NotImplementedException();
        }

        /// <summary>
        /// A unary expression.
        /// </summary>
        public sealed record class Unary(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IUnaryOperator Operator,
            Expr Operand) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Operator.ResultType;
        }

        /// <summary>
        /// A binary expression.
        /// </summary>
        public sealed record class Binary(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Left,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IBinaryOperator Operator,
            Expr Right) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Operator.ResultType;
        }

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed record class Relational(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Left,
            ImmutableArray<ComparisonElement> Comparisons) : Expr
        {
            // TODO
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => throw new NotImplementedException();
        }

        /// <summary>
        /// An assignment expression, including compound assignment.
        /// </summary>
        public sealed record class Assign(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Target,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IBinaryOperator? CompoundOperator,
            Expr Value) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Target.EvaluationType;
        }

        /// <summary>
        /// A short-cutting conjunction expression.
        /// </summary>
        public sealed record class And(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Left,
            Expr Right) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Bool;
        }

        /// <summary>
        /// A short-cutting disjunction expression.
        /// </summary>
        public sealed record class Or(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Left,
            Expr Right) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Bool;
        }

        /// <summary>
        /// A string expression composing string content and interpolation.
        /// </summary>
        public sealed record class String(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            ImmutableArray<StringPart> Parts) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.String;
        }

        /// <summary>
        /// A name reference expression.
        /// </summary>
        public sealed record class Reference(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.ITyped Symbol) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Symbol.Type;
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            [property: Ignore(IgnoreFlags.TransformerTransform)] string Value,
            [property: Ignore(IgnoreFlags.TransformerTransform)] int Cutoff) : StringPart;

        /// <summary>
        /// An interpolation hole.
        /// </summary>
        public sealed record class Interpolation(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Expr Expression) : StringPart;
    }

    /// <summary>
    /// A single comparison element in a comparison chain.
    /// </summary>
    public record class ComparisonElement(
        [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
        [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IBinaryOperator Operator,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree) : Stmt
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Ast.Decl Declaration) : Stmt;

        /// <summary>
        /// An expression statement.
        /// </summary>
        public new sealed record class Expr(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseTree? ParseTree,
            Ast.Expr Expression) : Stmt;
    }
}
