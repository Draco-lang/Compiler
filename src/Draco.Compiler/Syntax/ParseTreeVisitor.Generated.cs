namespace Draco.Compiler.Syntax;

internal partial interface IParseTreeVisitor<out T>
{
    public T Visit(Draco.Compiler.Syntax.ParseTree node);
    public T VisitCompilationUnit(Draco.Compiler.Syntax.ParseTree.CompilationUnit node);
    public T VisitDecl(Draco.Compiler.Syntax.ParseTree.Decl node);
    public T VisitUnexpectedDecl(Draco.Compiler.Syntax.ParseTree.Decl.Unexpected node);
    public T VisitFuncDecl(Draco.Compiler.Syntax.ParseTree.Decl.Func node);
    public T VisitLabelDecl(Draco.Compiler.Syntax.ParseTree.Decl.Label node);
    public T VisitVariableDecl(Draco.Compiler.Syntax.ParseTree.Decl.Variable node);
    public T VisitFuncParam(Draco.Compiler.Syntax.ParseTree.FuncParam node);
    public T VisitFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody node);
    public T VisitUnexpectedFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody.Unexpected node);
    public T VisitBlockBodyFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody.BlockBody node);
    public T VisitInlineBodyFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody.InlineBody node);
    public T VisitTypeExpr(Draco.Compiler.Syntax.ParseTree.TypeExpr node);
    public T VisitNameTypeExpr(Draco.Compiler.Syntax.ParseTree.TypeExpr.Name node);
    public T VisitTypeSpecifier(Draco.Compiler.Syntax.ParseTree.TypeSpecifier node);
    public T VisitStmt(Draco.Compiler.Syntax.ParseTree.Stmt node);
    public T VisitDeclStmt(Draco.Compiler.Syntax.ParseTree.Stmt.Decl node);
    public T VisitExprStmt(Draco.Compiler.Syntax.ParseTree.Stmt.Expr node);
    public T VisitExpr(Draco.Compiler.Syntax.ParseTree.Expr node);
    public T VisitUnexpectedExpr(Draco.Compiler.Syntax.ParseTree.Expr.Unexpected node);
    public T VisitUnitStmtExpr(Draco.Compiler.Syntax.ParseTree.Expr.UnitStmt node);
    public T VisitBlockExpr(Draco.Compiler.Syntax.ParseTree.Expr.Block node);
    public T VisitIfExpr(Draco.Compiler.Syntax.ParseTree.Expr.If node);
    public T VisitWhileExpr(Draco.Compiler.Syntax.ParseTree.Expr.While node);
    public T VisitGotoExpr(Draco.Compiler.Syntax.ParseTree.Expr.Goto node);
    public T VisitReturnExpr(Draco.Compiler.Syntax.ParseTree.Expr.Return node);
    public T VisitLiteralExpr(Draco.Compiler.Syntax.ParseTree.Expr.Literal node);
    public T VisitCallExpr(Draco.Compiler.Syntax.ParseTree.Expr.Call node);
    public T VisitNameExpr(Draco.Compiler.Syntax.ParseTree.Expr.Name node);
    public T VisitMemberAccessExpr(Draco.Compiler.Syntax.ParseTree.Expr.MemberAccess node);
    public T VisitUnaryExpr(Draco.Compiler.Syntax.ParseTree.Expr.Unary node);
    public T VisitBinaryExpr(Draco.Compiler.Syntax.ParseTree.Expr.Binary node);
    public T VisitRelationalExpr(Draco.Compiler.Syntax.ParseTree.Expr.Relational node);
    public T VisitGroupingExpr(Draco.Compiler.Syntax.ParseTree.Expr.Grouping node);
    public T VisitStringExpr(Draco.Compiler.Syntax.ParseTree.Expr.String node);
    public T VisitStringPart(Draco.Compiler.Syntax.ParseTree.StringPart node);
    public T VisitContentStringPart(Draco.Compiler.Syntax.ParseTree.StringPart.Content node);
    public T VisitInterpolationStringPart(Draco.Compiler.Syntax.ParseTree.StringPart.Interpolation node);
}

internal abstract partial class ParseTreeVisitorBase<T>
{
    protected virtual T Default => default!;
    public virtual T Visit(Draco.Compiler.Syntax.ParseTree node) => node switch
    {
        Draco.Compiler.Syntax.ParseTree.Decl n => this.VisitDecl(n),
        Draco.Compiler.Syntax.ParseTree.FuncBody n => this.VisitFuncBody(n),
        Draco.Compiler.Syntax.ParseTree.TypeExpr n => this.VisitTypeExpr(n),
        Draco.Compiler.Syntax.ParseTree.Stmt n => this.VisitStmt(n),
        Draco.Compiler.Syntax.ParseTree.Expr n => this.VisitExpr(n),
        Draco.Compiler.Syntax.ParseTree.StringPart n => this.VisitStringPart(n),
        Draco.Compiler.Syntax.ParseTree.CompilationUnit n => this.VisitCompilationUnit(n),
        Draco.Compiler.Syntax.ParseTree.FuncParam n => this.VisitFuncParam(n),
        Draco.Compiler.Syntax.ParseTree.ValueInitializer n => this.VisitValueInitializer(n),
        Draco.Compiler.Syntax.ParseTree.TypeSpecifier n => this.VisitTypeSpecifier(n),
        Draco.Compiler.Syntax.ParseTree.ElseClause n => this.VisitElseClause(n),
        Draco.Compiler.Syntax.ParseTree.BlockContents n => this.VisitBlockContents(n),
        Draco.Compiler.Syntax.ParseTree.ComparisonElement n => this.VisitComparisonElement(n),
        _ => throw new System.ArgumentOutOfRangeException(nameof(node)),
    };
    public virtual T VisitCompilationUnit(Draco.Compiler.Syntax.ParseTree.CompilationUnit node)
    {
        this.VisitValueArray(node.Declarations);
        return this.Default;
    }

    public virtual T VisitDecl(Draco.Compiler.Syntax.ParseTree.Decl node) => node switch
    {
        Draco.Compiler.Syntax.ParseTree.Decl.Unexpected n => this.VisitUnexpectedDecl(n),
        Draco.Compiler.Syntax.ParseTree.Decl.Func n => this.VisitFuncDecl(n),
        Draco.Compiler.Syntax.ParseTree.Decl.Label n => this.VisitLabelDecl(n),
        Draco.Compiler.Syntax.ParseTree.Decl.Variable n => this.VisitVariableDecl(n),
        _ => throw new System.ArgumentOutOfRangeException(nameof(node)),
    };
    public virtual T VisitUnexpectedDecl(Draco.Compiler.Syntax.ParseTree.Decl.Unexpected node)
    {
        this.VisitValueArray(node.Tokens);
        this.VisitValueArray(node.Diagnostics);
        return this.Default;
    }

    public virtual T VisitFuncDecl(Draco.Compiler.Syntax.ParseTree.Decl.Func node)
    {
        this.VisitToken(node.FuncKeyword);
        this.VisitToken(node.Identifier);
        this.VisitEnclosed(node.Params);
        if (node.ReturnType is not null)
            this.VisitTypeSpecifier(node.ReturnType);
        this.VisitFuncBody(node.Body);
        return this.Default;
    }

    public virtual T VisitLabelDecl(Draco.Compiler.Syntax.ParseTree.Decl.Label node)
    {
        this.VisitToken(node.Identifier);
        this.VisitToken(node.ColonToken);
        return this.Default;
    }

    public virtual T VisitVariableDecl(Draco.Compiler.Syntax.ParseTree.Decl.Variable node)
    {
        this.VisitToken(node.Keyword);
        this.VisitToken(node.Identifier);
        if (node.Type is not null)
            this.VisitTypeSpecifier(node.Type);
        if (node.Initializer is not null)
            this.VisitValueInitializer(node.Initializer);
        this.VisitToken(node.Semicolon);
        return this.Default;
    }

    public virtual T VisitFuncParam(Draco.Compiler.Syntax.ParseTree.FuncParam node)
    {
        this.VisitToken(node.Identifier);
        this.VisitTypeSpecifier(node.Type);
        return this.Default;
    }

    public virtual T VisitValueInitializer(Draco.Compiler.Syntax.ParseTree.ValueInitializer node)
    {
        this.VisitToken(node.AssignToken);
        this.VisitExpr(node.Value);
        return this.Default;
    }

    public virtual T VisitFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody node) => node switch
    {
        Draco.Compiler.Syntax.ParseTree.FuncBody.Unexpected n => this.VisitUnexpectedFuncBody(n),
        Draco.Compiler.Syntax.ParseTree.FuncBody.BlockBody n => this.VisitBlockBodyFuncBody(n),
        Draco.Compiler.Syntax.ParseTree.FuncBody.InlineBody n => this.VisitInlineBodyFuncBody(n),
        _ => throw new System.ArgumentOutOfRangeException(nameof(node)),
    };
    public virtual T VisitUnexpectedFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody.Unexpected node)
    {
        this.VisitValueArray(node.Tokens);
        this.VisitValueArray(node.Diagnostics);
        return this.Default;
    }

    public virtual T VisitBlockBodyFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody.BlockBody node)
    {
        this.VisitBlockExpr(node.Block);
        return this.Default;
    }

    public virtual T VisitInlineBodyFuncBody(Draco.Compiler.Syntax.ParseTree.FuncBody.InlineBody node)
    {
        this.VisitToken(node.AssignToken);
        this.VisitExpr(node.Expression);
        this.VisitToken(node.Semicolon);
        return this.Default;
    }

    public virtual T VisitTypeExpr(Draco.Compiler.Syntax.ParseTree.TypeExpr node) => node switch
    {
        Draco.Compiler.Syntax.ParseTree.TypeExpr.Name n => this.VisitNameTypeExpr(n),
        _ => throw new System.ArgumentOutOfRangeException(nameof(node)),
    };
    public virtual T VisitNameTypeExpr(Draco.Compiler.Syntax.ParseTree.TypeExpr.Name node)
    {
        this.VisitToken(node.Identifier);
        return this.Default;
    }

    public virtual T VisitTypeSpecifier(Draco.Compiler.Syntax.ParseTree.TypeSpecifier node)
    {
        this.VisitToken(node.ColonToken);
        this.VisitTypeExpr(node.Type);
        return this.Default;
    }

    public virtual T VisitStmt(Draco.Compiler.Syntax.ParseTree.Stmt node) => node switch
    {
        Draco.Compiler.Syntax.ParseTree.Stmt.Decl n => this.VisitDeclStmt(n),
        Draco.Compiler.Syntax.ParseTree.Stmt.Expr n => this.VisitExprStmt(n),
        _ => throw new System.ArgumentOutOfRangeException(nameof(node)),
    };
    public virtual T VisitDeclStmt(Draco.Compiler.Syntax.ParseTree.Stmt.Decl node)
    {
        this.VisitDecl(node.Declaration);
        return this.Default;
    }

    public virtual T VisitExprStmt(Draco.Compiler.Syntax.ParseTree.Stmt.Expr node)
    {
        this.VisitExpr(node.Expression);
        if (node.Semicolon is not null)
            this.VisitToken(node.Semicolon);
        return this.Default;
    }

    public virtual T VisitExpr(Draco.Compiler.Syntax.ParseTree.Expr node) => node switch
    {
        Draco.Compiler.Syntax.ParseTree.Expr.Unexpected n => this.VisitUnexpectedExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.UnitStmt n => this.VisitUnitStmtExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Block n => this.VisitBlockExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.If n => this.VisitIfExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.While n => this.VisitWhileExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Goto n => this.VisitGotoExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Return n => this.VisitReturnExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Literal n => this.VisitLiteralExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Call n => this.VisitCallExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Name n => this.VisitNameExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.MemberAccess n => this.VisitMemberAccessExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Unary n => this.VisitUnaryExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Binary n => this.VisitBinaryExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Relational n => this.VisitRelationalExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.Grouping n => this.VisitGroupingExpr(n),
        Draco.Compiler.Syntax.ParseTree.Expr.String n => this.VisitStringExpr(n),
        _ => throw new System.ArgumentOutOfRangeException(nameof(node)),
    };
    public virtual T VisitUnexpectedExpr(Draco.Compiler.Syntax.ParseTree.Expr.Unexpected node)
    {
        this.VisitValueArray(node.Tokens);
        this.VisitValueArray(node.Diagnostics);
        return this.Default;
    }

    public virtual T VisitUnitStmtExpr(Draco.Compiler.Syntax.ParseTree.Expr.UnitStmt node)
    {
        this.VisitStmt(node.Statement);
        return this.Default;
    }

    public virtual T VisitBlockExpr(Draco.Compiler.Syntax.ParseTree.Expr.Block node)
    {
        this.VisitEnclosed(node.Enclosed);
        return this.Default;
    }

    public virtual T VisitIfExpr(Draco.Compiler.Syntax.ParseTree.Expr.If node)
    {
        this.VisitToken(node.IfKeyword);
        this.VisitEnclosed(node.Condition);
        this.VisitExpr(node.Then);
        if (node.Else is not null)
            this.VisitElseClause(node.Else);
        return this.Default;
    }

    public virtual T VisitWhileExpr(Draco.Compiler.Syntax.ParseTree.Expr.While node)
    {
        this.VisitToken(node.WhileKeyword);
        this.VisitEnclosed(node.Condition);
        this.VisitExpr(node.Expression);
        return this.Default;
    }

    public virtual T VisitGotoExpr(Draco.Compiler.Syntax.ParseTree.Expr.Goto node)
    {
        this.VisitToken(node.GotoKeyword);
        this.VisitToken(node.Identifier);
        return this.Default;
    }

    public virtual T VisitReturnExpr(Draco.Compiler.Syntax.ParseTree.Expr.Return node)
    {
        this.VisitToken(node.ReturnKeyword);
        if (node.Expression is not null)
            this.VisitExpr(node.Expression);
        return this.Default;
    }

    public virtual T VisitLiteralExpr(Draco.Compiler.Syntax.ParseTree.Expr.Literal node)
    {
        this.VisitToken(node.Value);
        return this.Default;
    }

    public virtual T VisitCallExpr(Draco.Compiler.Syntax.ParseTree.Expr.Call node)
    {
        this.VisitExpr(node.Called);
        this.VisitEnclosed(node.Args);
        return this.Default;
    }

    public virtual T VisitNameExpr(Draco.Compiler.Syntax.ParseTree.Expr.Name node)
    {
        this.VisitToken(node.Identifier);
        return this.Default;
    }

    public virtual T VisitMemberAccessExpr(Draco.Compiler.Syntax.ParseTree.Expr.MemberAccess node)
    {
        this.VisitExpr(node.Object);
        this.VisitToken(node.DotToken);
        this.VisitToken(node.MemberName);
        return this.Default;
    }

    public virtual T VisitUnaryExpr(Draco.Compiler.Syntax.ParseTree.Expr.Unary node)
    {
        this.VisitToken(node.Operator);
        this.VisitExpr(node.Operand);
        return this.Default;
    }

    public virtual T VisitBinaryExpr(Draco.Compiler.Syntax.ParseTree.Expr.Binary node)
    {
        this.VisitExpr(node.Left);
        this.VisitToken(node.Operator);
        this.VisitExpr(node.Right);
        return this.Default;
    }

    public virtual T VisitRelationalExpr(Draco.Compiler.Syntax.ParseTree.Expr.Relational node)
    {
        this.VisitExpr(node.Left);
        this.VisitValueArray(node.Comparisons);
        return this.Default;
    }

    public virtual T VisitGroupingExpr(Draco.Compiler.Syntax.ParseTree.Expr.Grouping node)
    {
        this.VisitEnclosed(node.Expression);
        return this.Default;
    }

    public virtual T VisitStringExpr(Draco.Compiler.Syntax.ParseTree.Expr.String node)
    {
        this.VisitToken(node.OpenQuotes);
        this.VisitValueArray(node.Parts);
        this.VisitToken(node.CloseQuotes);
        return this.Default;
    }

    public virtual T VisitElseClause(Draco.Compiler.Syntax.ParseTree.ElseClause node)
    {
        this.VisitToken(node.ElseToken);
        this.VisitExpr(node.Expression);
        return this.Default;
    }

    public virtual T VisitBlockContents(Draco.Compiler.Syntax.ParseTree.BlockContents node)
    {
        this.VisitValueArray(node.Statements);
        if (node.Value is not null)
            this.VisitExpr(node.Value);
        return this.Default;
    }

    public virtual T VisitComparisonElement(Draco.Compiler.Syntax.ParseTree.ComparisonElement node)
    {
        this.VisitToken(node.Operator);
        this.VisitExpr(node.Right);
        return this.Default;
    }

    public virtual T VisitStringPart(Draco.Compiler.Syntax.ParseTree.StringPart node) => node switch
    {
        Draco.Compiler.Syntax.ParseTree.StringPart.Content n => this.VisitContentStringPart(n),
        Draco.Compiler.Syntax.ParseTree.StringPart.Interpolation n => this.VisitInterpolationStringPart(n),
        _ => throw new System.ArgumentOutOfRangeException(nameof(node)),
    };
    public virtual T VisitContentStringPart(Draco.Compiler.Syntax.ParseTree.StringPart.Content node)
    {
        this.VisitToken(node.Token);
        this.VisitValueArray(node.Diagnostics);
        return this.Default;
    }

    public virtual T VisitInterpolationStringPart(Draco.Compiler.Syntax.ParseTree.StringPart.Interpolation node)
    {
        this.VisitToken(node.OpenToken);
        this.VisitExpr(node.Expression);
        this.VisitToken(node.CloseToken);
        return this.Default;
    }
}

