using System;
using System.Collections.Generic;
using System.Linq;
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
    T VisitCallExpr(Expr.Call expr);
    T VisitNameExpr(Expr.Name expr);
    T VisitMemberAccessExpr(Expr.MemberAccess expr);
    T VisitUnaryExpr(Expr.Unary expr);
    T VisitBinaryExpr(Expr.Binary expr);
    T VisitGroupingExpr(Expr.Grouping expr);
}

/// <summary>
/// Provides a base implementation of <see cref="IParseTreeVisitor{T}"/>.
/// </summary>
/// <typeparam name="T">The return type of the visitor.</typeparam>
internal abstract partial class BaseParseTreeVisitor<T> : IParseTreeVisitor<T>
{
    /// <summary>
    /// The default value returned by every non-overwritten visit method.
    /// </summary>
    public virtual T Default => default!;

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
        _ => throw new InvalidOperationException(),
    };

    public virtual T VisitCompilationUnit(CompilationUnit compilationUnit)
    {
        this.VisitEnumerable(compilationUnit.Declarations);
        return this.Default;
    }

    public virtual T VisitDecl(Decl decl) => decl switch
    {
        Decl.Func func => this.VisitFuncDecl(func),
        Decl.Label label => this.VisitLabelDecl(label),
        Decl.Variable variable => this.VisitVariableDecl(variable),
        _ => throw new InvalidOperationException(),
    };

    public virtual T VisitFuncDecl(Decl.Func decl)
    {
        this.VisitPunctuatedList(decl.Params.Value);
        this.VisitNullable(decl.ReturnType);
        this.VisitFuncBody(decl.Body);
        return this.Default;
    }

    public virtual T VisitLabelDecl(Decl.Label decl) => this.Default;

    public virtual T VisitVariableDecl(Decl.Variable decl)
    {
        this.VisitNullable(decl.Type);
        this.VisitNullable(decl.Initializer?.Expression);
        return this.Default;
    }

    public virtual T VisitFuncParam(FuncParam param) =>
        this.VisitTypeSpecifier(param.Type);

    public virtual T VisitFuncBody(FuncBody body) => body switch
    {
        FuncBody.BlockBody block => this.VisitBlockFuncBody(block),
        FuncBody.InlineBody inline => this.VisitInlineFuncBody(inline),
        _ => throw new InvalidOperationException(),
    };

    public virtual T VisitBlockFuncBody(FuncBody.BlockBody body) =>
        this.VisitBlockExpr(body.Block);

    public virtual T VisitInlineFuncBody(FuncBody.InlineBody body) =>
        this.VisitExpr(body.Expression);

    public virtual T VisitTypeExpr(TypeExpr typeExpr) => typeExpr switch
    {
        TypeExpr.Name name => this.VisitNameTypeExpr(name),
        _ => throw new InvalidOperationException(),
    };

    public virtual T VisitNameTypeExpr(TypeExpr.Name typeExpr) =>
        this.Default;

    public virtual T VisitTypeSpecifier(TypeSpecifier specifier)
    {
        this.VisitTypeExpr(specifier.Type);
        return this.Default;
    }

    public virtual T VisitStmt(Stmt stmt) => stmt switch
    {
        Stmt.Decl decl => this.VisitDeclStmt(decl),
        Stmt.Expr expr => this.VisitExprStmt(expr),
        _ => throw new InvalidOperationException(),
    };

    public virtual T VisitDeclStmt(Stmt.Decl stmt) =>
        this.VisitDecl(stmt.Declaration);

    public virtual T VisitExprStmt(Stmt.Expr stmt) =>
        this.VisitExpr(stmt.Expression);

    public virtual T VisitExpr(Expr expr) => expr switch
    {
        Expr.Block block => this.VisitBlockExpr(block),
        Expr.If @if => this.VisitIfExpr(@if),
        Expr.While @while => this.VisitWhileExpr(@while),
        Expr.Goto @goto => this.VisitGotoExpr(@goto),
        Expr.Return @return => this.VisitReturnExpr(@return),
        Expr.Literal literal => this.VisitLiteralExpr(literal),
        Expr.Call call => this.VisitCallExpr(call),
        Expr.Name name => this.VisitNameExpr(name),
        Expr.MemberAccess memberAccess => this.VisitMemberAccessExpr(memberAccess),
        Expr.Unary unary => this.VisitUnaryExpr(unary),
        Expr.Binary binary => this.VisitBinaryExpr(binary),
        Expr.Grouping grouping => this.VisitGroupingExpr(grouping),
        _ => throw new InvalidOperationException(),
    };

    public virtual T VisitBlockExpr(Expr.Block expr)
    {
        var (stmts, value) = expr.Enclosed.Value;
        this.VisitEnumerable(stmts);
        this.VisitNullable(value);
        return this.Default;
    }

    public virtual T VisitIfExpr(Expr.If expr)
    {
        this.VisitExpr(expr.Condition.Value);
        this.VisitExpr(expr.Then);
        this.VisitNullable(expr.Else?.Expression);
        return this.Default;
    }

    public virtual T VisitWhileExpr(Expr.While expr)
    {
        this.VisitExpr(expr.Condition.Value);
        this.VisitExpr(expr.Expression);
        return this.Default;
    }

    public virtual T VisitGotoExpr(Expr.Goto expr) => this.Default;

    public virtual T VisitReturnExpr(Expr.Return expr) =>
        this.VisitNullable(expr.Expression);

    public virtual T VisitLiteralExpr(Expr.Literal expr) => this.Default;

    public virtual T VisitCallExpr(Expr.Call expr)
    {
        this.VisitExpr(expr.Called);
        this.VisitPunctuatedList(expr.Args.Value);
        return this.Default;
    }

    public virtual T VisitNameExpr(Expr.Name expr) => this.Default;

    public virtual T VisitMemberAccessExpr(Expr.MemberAccess expr) =>
        this.VisitExpr(expr.Object);

    public virtual T VisitUnaryExpr(Expr.Unary expr) =>
        this.VisitExpr(expr.Operand);

    public virtual T VisitBinaryExpr(Expr.Binary expr)
    {
        this.VisitExpr(expr.Left);
        this.VisitExpr(expr.Right);
        return this.Default;
    }

    public virtual T VisitGroupingExpr(Expr.Grouping expr) =>
        this.VisitExpr(expr.Expression.Value);
}

// Utility functions
internal abstract partial class BaseParseTreeVisitor<T>
{
    /// <summary>
    /// Visits every element in an enumerable of nodes.
    /// </summary>
    protected T VisitEnumerable(IEnumerable<ParseTree> elements)
    {
        foreach (var item in elements) this.VisitNode(item);
        return this.Default;
    }

    /// <summary>
    /// Visits every element of a punctuated list of nodes.
    /// </summary>
    protected T VisitPunctuatedList<TElement>(PunctuatedList<TElement> list)
        where TElement : ParseTree
    {
        foreach (var (item, _) in list.Elements) this.VisitNode(item);
        return this.Default;
    }

    /// <summary>
    /// Visits a node if it's not <see langword="null"/>.
    /// </summary>
    protected T VisitNullable(ParseTree? element) => element is null
        ? this.Default
        : this.VisitNode(element);
}
