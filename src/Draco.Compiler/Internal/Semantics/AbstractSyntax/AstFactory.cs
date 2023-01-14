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
        ParseNode: null,
        LabelSymbol: symbol);

    public static Decl Var(ISymbol.IVariable varSymbol, Expr value) => new Decl.Variable(
        ParseNode: null,
        DeclarationSymbol: varSymbol,
        Value: value);

    public static Stmt Stmt(Decl decl) => new Stmt.Decl(
        ParseNode: null,
        Declaration: decl);

    public static Stmt Stmt(Expr expr) => new Stmt.Expr(
        ParseNode: null,
        Expression: expr);

    public static Expr Block(IEnumerable<Stmt> stmts, Expr? value = null) => new Expr.Block(
        ParseNode: null,
        Statements: stmts.ToImmutableArray(),
        Value: value ?? Expr.Unit.Default);

    public static Expr Block(params Stmt[] stmts) => Block(stmts as IEnumerable<Stmt>);

    public static Expr Goto(ISymbol.ILabel labelSymbol) => new Expr.Goto(
        ParseNode: null,
        Target: labelSymbol);

    public static Expr If(Expr condition, Expr then, Expr @else) => new Expr.If(
        ParseNode: null,
        Condition: condition,
        Then: then,
        Else: @else ?? Expr.Unit.Default);

    public static Stmt If(Expr condition, Expr then) => Stmt(If(
        condition: condition,
        then: Block(Stmt(then)),
        @else: Expr.Unit.Default));

    public static Expr Unary(ISymbol.IFunction op, Expr subexpr) => new Expr.Unary(
        ParseNode: null,
        Operator: op,
        Operand: subexpr);

    public static Expr Binary(Expr left, ISymbol.IFunction op, Expr right) => new Expr.Binary(
        ParseNode: null,
        Left: left,
        Operator: op,
        Right: right);

    public static Expr Assign(LValue target, Expr value) => new Expr.Assign(
        ParseNode: null,
        Target: target,
        CompoundOperator: null,
        Value: value);

    public static Expr And(Expr left, Expr right) => new Expr.And(
        ParseNode: null,
        Left: left,
        Right: right);

    public static Expr Or(Expr left, Expr right) => new Expr.Or(
        ParseNode: null,
        Left: left,
        Right: right);

    public static Expr Not(Expr subexpr) => Unary(
        op: Intrinsics.Operators.Not_Bool,
        subexpr: subexpr);

    public static Expr Reference(ISymbol.ITyped symbol) => new Expr.Reference(
        ParseNode: null,
        Symbol: symbol);

    public static Expr Bool(bool value) => new Expr.Literal(
        ParseNode: null,
        Value: value,
        Type: Type.Bool);

    public static LValue LValueReference(ISymbol.IVariable symbol) => new LValue.Reference(
        ParseNode: null,
        Symbol: symbol);
}
