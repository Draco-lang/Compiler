using System;
using static Draco.Compiler.Syntax.ParseTree;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Defines a visitor of a <see cref="ParseTree"/>.
/// </summary>
/// <typeparam name="T">The return type of the visitor.</typeparam>
internal interface IParseTreeVisitor<out T>
{
    T VisitCompilationUnit(CompilationUnit compilationUnit);

    T VisitDecl(Decl decl);
    T VisitFuncDecl(Decl.Func decl);
    T VisitLabelDecl(Decl.Label decl);
    T VisitVariableDecl(Decl.Variable decl);

    T VisitFuncParam(FuncParam param);

    T VisitFuncBody(FuncBody body);
    T VisitBlockFuncBody(FuncBody.BlockBody body);
    T VisitInlineFuncBody(FuncBody.InlineBody body);

    T VisitTypeExpr(TypeExpr typeExpr);
    T VisitNameTypeExpr(TypeExpr.Name typeExpr);

    T VisitTypeSpecifier(TypeSpecifier specifier);

    T VisitStmt(Stmt stmt);
    T VisitDeclStmt(Stmt.Decl stmt);
    T VisitExprStmt(Stmt.Expr stmt);

    T VisitExpr(Expr expr);
    T VisitBlockExpr(Expr.Block expr);
    T VisitIfExpr(Expr.If expr);
    T VisitWhileExpr(Expr.While expr);
    T VisitGotoExpr(Expr.Goto expr);
    T VisitReturnExpr(Expr.Return expr);
    T VisitLiteralExpr(Expr.Literal expr);
    T VisitFuncCallExpr(Expr.FuncCall expr);
    T VisitIndexExpr(Expr.Index expr);
    T VisitVariableExpr(Expr.Variable expr);
    T VisitMemberAccessExpr(Expr.MemberAccess expr);
    T VisitUnaryExpr(Expr.Unary expr);
    T VisitBinaryExpr(Expr.Binary expr);
    T VisitGroupingExpr(Expr.Grouping expr);
}

/// <summary>
/// Provides a base implementation of <see cref="IParseTreeVisitor{T}"/>.
/// </summary>
/// <typeparam name="T">The return type of the visitor.</typeparam>
internal abstract class BaseParseTreeVisitor<T> : IParseTreeVisitor<T>
{
    public virtual T VisitNode(ParseTree node) => node switch
    {
        CompilationUnit compilationUnit => this.VisitCompilationUnit(compilationUnit),
        Decl decl => this.VisitDecl(decl),
        FuncParam param => this.VisitFuncParam(param),
        FuncBody body => this.VisitFuncBody(body),
        TypeExpr typeExpr => this.VisitTypeExpr(typeExpr),
        TypeSpecifier specifier => this.VisitTypeSpecifier(specifier),
        Stmt stmt => this.VisitStmt(stmt),
        Expr expr => this.VisitExpr(expr),

        _ => throw new InvalidOperationException()
    };

    public abstract T VisitCompilationUnit(CompilationUnit compilationUnit);

    public virtual T VisitDecl(Decl decl) => decl switch
    {
        Decl.Func func => this.VisitFuncDecl(func),
        Decl.Label label => this.VisitLabelDecl(label),
        Decl.Variable variable => this.VisitVariableDecl(variable),

        _ => throw new InvalidOperationException()
    };
    public abstract T VisitFuncDecl(Decl.Func decl);
    public abstract T VisitLabelDecl(Decl.Label decl);
    public abstract T VisitVariableDecl(Decl.Variable decl);

    public abstract T VisitFuncParam(FuncParam param);

    public virtual T VisitFuncBody(FuncBody body) => body switch
    {
        FuncBody.BlockBody block => this.VisitBlockFuncBody(block),
        FuncBody.InlineBody inline => this.VisitInlineFuncBody(inline),

        _ => throw new InvalidOperationException()
    };
    public abstract T VisitBlockFuncBody(FuncBody.BlockBody body);
    public abstract T VisitInlineFuncBody(FuncBody.InlineBody body);

    public virtual T VisitTypeExpr(TypeExpr typeExpr) => typeExpr switch
    {
        TypeExpr.Name name => this.VisitNameTypeExpr(name),

        _ => throw new InvalidOperationException()
    };
    public abstract T VisitNameTypeExpr(TypeExpr.Name typeExpr);

    public abstract T VisitTypeSpecifier(TypeSpecifier specifier);

    public virtual T VisitStmt(Stmt stmt) => stmt switch
    {
        Stmt.Decl decl => this.VisitDeclStmt(decl),
        Stmt.Expr expr => this.VisitExprStmt(expr),

        _ => throw new InvalidOperationException()
    };
    public abstract T VisitDeclStmt(Stmt.Decl stmt);
    public abstract T VisitExprStmt(Stmt.Expr stmt);

    public virtual T VisitExpr(Expr expr) => expr switch
    {
        Expr.Block block => this.VisitBlockExpr(block),
        Expr.If @if => this.VisitIfExpr(@if),
        Expr.While @while => this.VisitWhileExpr(@while),
        Expr.Goto @goto => this.VisitGotoExpr(@goto),
        Expr.Return @return => this.VisitReturnExpr(@return),
        Expr.Literal literal => this.VisitLiteralExpr(literal),
        Expr.FuncCall funcCall => this.VisitFuncCallExpr(funcCall),
        Expr.Index index => this.VisitIndexExpr(index),
        Expr.Variable variable => this.VisitVariableExpr(variable),
        Expr.MemberAccess memberAccess => this.VisitMemberAccessExpr(memberAccess),
        Expr.Unary unary => this.VisitUnaryExpr(unary),
        Expr.Binary binary => this.VisitBinaryExpr(binary),
        Expr.Grouping grouping => this.VisitGroupingExpr(grouping),

        _ => throw new InvalidOperationException()
    };
    public abstract T VisitBlockExpr(Expr.Block expr);
    public abstract T VisitIfExpr(Expr.If expr);
    public abstract T VisitWhileExpr(Expr.While expr);
    public abstract T VisitGotoExpr(Expr.Goto expr);
    public abstract T VisitReturnExpr(Expr.Return expr);
    public abstract T VisitLiteralExpr(Expr.Literal expr);
    public abstract T VisitFuncCallExpr(Expr.FuncCall expr);
    public abstract T VisitIndexExpr(Expr.Index expr);
    public abstract T VisitVariableExpr(Expr.Variable expr);
    public abstract T VisitMemberAccessExpr(Expr.MemberAccess expr);
    public abstract T VisitUnaryExpr(Expr.Unary expr);
    public abstract T VisitBinaryExpr(Expr.Binary expr);
    public abstract T VisitGroupingExpr(Expr.Grouping expr);
}
