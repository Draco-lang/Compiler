using System;
using System.Collections.Immutable;
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
    public abstract ParseNode? ParseNode { get; init; }

    /// <summary>
    /// An entire compilation unit.
    /// </summary>
    public sealed record class CompilationUnit(
        [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
        ImmutableArray<Decl> Declarations) : Ast;

    /// <summary>
    /// A declaration, either top-level or as a statement.
    /// </summary>
    public abstract record class Decl : Ast
    {
        /// <summary>
        /// An unexpected declaration.
        /// </summary>
        public sealed record class Unexpected(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode) : Decl;

        /// <summary>
        /// A function declaration.
        /// </summary>
        public sealed record class Func(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.ILabel LabelSymbol) : Decl;

        /// <summary>
        /// A variable declaration.
        /// </summary>
        public sealed record class Variable(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
        /// An unexpected expression.
        /// </summary>
        public sealed record class Unexpected(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode) : Expr
        {
            public override Type EvaluationType => Type.Error.Empty;
        }

        /// <summary>
        /// An expression representing unitary value.
        /// </summary>
        public sealed record class Unit(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode) : Expr
        {
            /// <summary>
            /// A default unit value without a parse tree.
            /// </summary>
            public static Unit Default { get; } = new(ParseNode: null);

            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A block expression.
        /// </summary>
        public sealed record class Block(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            [property: Ignore(IgnoreFlags.TransformerTransform)] object? Value,
            [property: Ignore(IgnoreFlags.TransformerTransform)] Type Type) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType { get; } = Type;
        }

        /// <summary>
        /// An if-expression with an optional else clause.
        /// </summary>
        public sealed record class If(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            Expr Condition,
            Expr Expression,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.ILabel BreakLabel,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.ILabel ContinueLabel) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.Unit;
        }

        /// <summary>
        /// A goto-expression.
        /// </summary>
        public sealed record class Goto(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IFunction Operator,
            Expr Operand) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Operator.ReturnType;
        }

        /// <summary>
        /// A binary expression.
        /// </summary>
        public sealed record class Binary(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            Expr Left,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IFunction Operator,
            Expr Right) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Operator.ReturnType;
        }

        /// <summary>
        /// A relational expression chain.
        /// </summary>
        public sealed record class Relational(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            LValue Target,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IFunction? CompoundOperator,
            Expr Value) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Target.EvaluationType;
        }

        /// <summary>
        /// A short-cutting conjunction expression.
        /// </summary>
        public sealed record class And(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            ImmutableArray<StringPart> Parts) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => Type.String;
        }

        /// <summary>
        /// A name reference expression.
        /// </summary>
        public sealed record class Reference(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.ITyped Symbol) : Expr
        {
            [Ignore(IgnoreFlags.TransformerAll)]
            public override Type EvaluationType => this.Symbol.Type;
        }
    }

    /// <summary>
    /// A value appearing on the left side of assignment.
    /// </summary>
    public abstract record class LValue : Ast
    {
        /// <summary>
        /// The type the lvalue references.
        /// </summary>
        [Ignore(IgnoreFlags.TransformerAll)]
        public abstract Type EvaluationType { get; }

        /// <summary>
        /// An unexpected lvalue.
        /// </summary>
        public sealed record class Unexpected(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode) : LValue
        {
            public override Type EvaluationType => Type.Error.Empty;
        }

        /// <summary>
        /// A name reference.
        /// </summary>
        public sealed record class Reference(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IVariable Symbol) : LValue
        {
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            [property: Ignore(IgnoreFlags.TransformerTransform)] string Value) : StringPart;

        /// <summary>
        /// An interpolation hole.
        /// </summary>
        public sealed record class Interpolation(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            Expr Expression) : StringPart;
    }

    /// <summary>
    /// A single comparison element in a comparison chain.
    /// </summary>
    public sealed record class ComparisonElement(
        [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
        [property: Ignore(IgnoreFlags.TransformerTransform)] ISymbol.IFunction Operator,
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
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode) : Stmt
        {
            /// <summary>
            /// A default instance to use.
            /// </summary>
            public static NoOp Default { get; } = new(ParseNode: null);
        }

        /// <summary>
        /// A declaration statement.
        /// </summary>
        public new sealed record class Decl(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            Ast.Decl Declaration) : Stmt;

        /// <summary>
        /// An expression statement.
        /// </summary>
        public new sealed record class Expr(
            [property: Ignore(IgnoreFlags.TransformerTransform)] ParseNode? ParseNode,
            Ast.Expr Expression) : Stmt;
    }
}
