using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Semantics.Symbols;
using static Draco.Compiler.Internal.Semantics.AbstractSyntax.Ast;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Factory helpers for synthetizing AST nodes.
/// </summary>
internal static class AstFactory
{
    public static Decl Label(Symbol symbol) => new Decl.Label(
        ParseTree: null,
        LabelSymbol: symbol);

    public static Decl Var(Symbol.IVariable varSymbol, Expr value) => new Decl.Variable(
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

    public static Expr Goto(Symbol labelSymbol) => new Expr.Goto(
        ParseTree: null,
        Target: labelSymbol);

    public static Expr If(Expr condition, Expr then, Expr? @else = null) => new Expr.If(
        ParseTree: null,
        Condition: condition,
        Then: then,
        Else: @else ?? Expr.Unit.Default);

    public static Expr Unary(Symbol.IOperator op, Expr subexpr) => new Expr.Unary(
        ParseTree: null,
        Operator: op,
        Operand: subexpr);

    public static Expr Binary(Expr left, Symbol.IOperator op, Expr right) => new Expr.Binary(
        ParseTree: null,
        Left: left,
        Operator: op,
        Right: right);

    public static Expr Reference(Symbol symbol) => new Expr.Reference(
        ParseTree: null,
        Symbol: symbol);
}
