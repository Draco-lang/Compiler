namespace Draco.Compiler.Syntax;

internal partial interface IParseTreeVisitor<out T>
{
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
