using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Semantics.Types;
using static Draco.Compiler.Internal.Semantics.AbstractSyntax.Ast;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Factory helpers for synthetizing AST nodes.
/// </summary>
internal static class AstFactory
{
    public static Decl Label(ISymbol.ILabel symbol) => new Decl.Label(
        ParseTree: null,
        LabelSymbol: symbol);

    public static Decl Var(ISymbol.IVariable varSymbol, Expr value) => new Decl.Variable(
        ParseTree: null,
        DeclarationSymbol: varSymbol,
        Value: value);

    public static Stmt Stmt(Decl decl) => new Stmt.Decl(
        ParseTree: null,
        Declaration: decl);

    public static Stmt Stmt(Expr expr) => new Stmt.Expr(
        ParseTree: null,
        Expression: expr);

    public static Expr Block(IEnumerable<Stmt> stmts, Expr? value = null) => new Expr.Block(
        ParseTree: null,
        Statements: stmts.ToImmutableArray(),
        Value: value ?? Expr.Unit.Default);

    public static Expr Block(params Stmt[] stmts) => Block(stmts as IEnumerable<Stmt>);

    public static Expr Goto(ISymbol.ILabel labelSymbol) => new Expr.Goto(
        ParseTree: null,
        Target: labelSymbol);

    public static Expr If(Expr condition, Expr then, Expr @else) => new Expr.If(
        ParseTree: null,
        Condition: condition,
        Then: then,
        Else: @else ?? Expr.Unit.Default);

    public static Stmt If(Expr condition, Expr then) => Stmt(If(
        condition: condition,
        then: Block(Stmt(then)),
        @else: Expr.Unit.Default));

    public static Expr Unary(ISymbol.IUnaryOperator op, Expr subexpr) => new Expr.Unary(
        ParseTree: null,
        Operator: op,
        Operand: subexpr);

    public static Expr Binary(Expr left, ISymbol.IBinaryOperator op, Expr right) => new Expr.Binary(
        ParseTree: null,
        Left: left,
        Operator: op,
        Right: right);

    public static Expr Assign(Expr target, Expr value) => new Expr.Assign(
        ParseTree: null,
        Target: target,
        CompoundOperator: null,
        Value: value);

    public static Expr And(Expr left, Expr right) => new Expr.And(
        ParseTree: null,
        Left: left,
        Right: right);

    public static Expr Or(Expr left, Expr right) => new Expr.Or(
        ParseTree: null,
        Left: left,
        Right: right);

    public static Expr Not(Expr subexpr) => Unary(
        op: Intrinsics.Operators.Not_Bool,
        subexpr: subexpr);

    public static Expr Reference(ISymbol.ITyped symbol) => new Expr.Reference(
        ParseTree: null,
        Symbol: symbol);

    public static Expr Bool(bool value) => new Expr.Literal(
        ParseTree: null,
        Value: value,
        Type: Type.Bool);
}
