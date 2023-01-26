using System;
using System.Linq;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Api.Syntax;
using System.Collections.Immutable;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;
using System.Diagnostics;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Computations for building the AST.
/// </summary>
internal static class ParseTreeToAst
{
    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="ast">The <see cref="SyntaxNode"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="ast"/>.</returns>
    public static Ast ToAst(QueryDatabase db, SyntaxNode ast) => ast switch
    {
        CompilationUnitSyntax cu => ToAstCompilationUnit(db, cu),
        DeclarationSyntax decl => ToAstDecl(db, decl),
        StatementSyntax stmt => ToAstStmt(db, stmt),
        ExpressionSyntax expr => ToAstExpr(db, expr),
        _ => throw new ArgumentOutOfRangeException(nameof(ast)),
    };

    /// <summary>
    /// Builds an <see cref="Ast.Decl"/> from the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="decl">The <see cref="SyntaxNode"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="decl"/>.</returns>
    public static Ast.Decl ToAstDecl(QueryDatabase db, DeclarationSyntax decl) => db.GetOrUpdate(
        decl,
        Ast.Decl (decl) => decl switch
        {
            // TODO: Eliminate the ?? pattern everywhere by making the API use optional
            // TODO: Eliminate the null ? null : ... pattern everywhere by making the API use optional

            UnexpectedDeclarationSyntax u => new Ast.Decl.Unexpected(u),
            FunctionDeclarationSyntax func => new Ast.Decl.Func(
                SyntaxNode: func,
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.IFunction>(db, func),
                Body: ToAstFuncBody(db, func.Body)),
            LabelDeclarationSyntax label => new Ast.Decl.Label(
                SyntaxNode: label,
                LabelSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.ILabel>(db, label)),
            VariableDeclarationSyntax var => new Ast.Decl.Variable(
                SyntaxNode: var,
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.IVariable>(db, var),
                Value: var.Value is null ? null : ToAstExpr(db, var.Value.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(decl)),
        });

    /// <summary>
    /// Builds an <see cref="Ast.Stmt"/> from the given <see cref="StatementSyntax"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="stmt">The <see cref="StatementSyntax"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="stmt"/>.</returns>
    public static Ast.Stmt ToAstStmt(QueryDatabase db, StatementSyntax stmt) => db.GetOrUpdate(
        stmt,
        Ast.Stmt (stmt) => stmt switch
        {
            UnexpectedStatementSyntax u => new Ast.Stmt.Unexpected(
                SyntaxNode: u),
            DeclarationStatementSyntax d => new Ast.Stmt.Decl(
                SyntaxNode: d,
                Declaration: ToAstDecl(db, d.Declaration)),
            ExpressionStatementSyntax expr => new Ast.Stmt.Expr(
                SyntaxNode: expr,
                Expression: ToAstExpr(db, expr.Expression)),
            _ => throw new ArgumentOutOfRangeException(nameof(stmt)),
        });

    /// <summary>
    /// Builds an <see cref="Ast.Expr"/> from the given <see cref="ExpressionSyntax"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ExpressionSyntax"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="expr"/>.</returns>
    public static Ast.Expr ToAstExpr(QueryDatabase db, ExpressionSyntax expr) => db.GetOrUpdate(
        expr,
        Ast.Expr (expr) => expr switch
        {
            UnexpectedExpressionSyntax u => new Ast.Expr.Unexpected(u),
            GroupingExpressionSyntax g => ToAstExpr(db, g.Expression),
            ReturnExpressionSyntax ret => new Ast.Expr.Return(
                SyntaxNode: ret,
                Expression: ret.Value is null ? Ast.Expr.Unit.Default : ToAstExpr(db, ret.Value)),
            GotoExpressionSyntax g => new Ast.Expr.Goto(
                SyntaxNode: g,
                Target: SymbolResolution.GetReferencedSymbol<ISymbol.ILabel>(db, g.Target)),
            NameExpressionSyntax name => new Ast.Expr.Reference(
                SyntaxNode: name,
                Symbol: SymbolResolution.GetReferencedSymbol<ISymbol.ITyped>(db, name)),
            IfExpressionSyntax @if => new Ast.Expr.If(
                SyntaxNode: @if,
                Condition: ToAstExpr(db, @if.Condition),
                Then: ToAstExpr(db, @if.Then),
                Else: @if.Else is null ? Ast.Expr.Unit.Default : ToAstExpr(db, @if.Else.Expression)),
            WhileExpressionSyntax @while => new Ast.Expr.While(
                SyntaxNode: @while,
                Condition: ToAstExpr(db, @while.Condition),
                Expression: ToAstExpr(db, @while.Then),
                // TODO: maybe solve it with one call
                BreakLabel: SymbolResolution.GetBreakAndContinueLabels(db, @while).Break,
                ContinueLabel: SymbolResolution.GetBreakAndContinueLabels(db, @while).Continue),
            BlockExpressionSyntax block => new Ast.Expr.Block(
                SyntaxNode: block,
                Statements: block.Statements.Select(s => ToAstStmt(db, s)).ToImmutableArray(),
                Value: block.Value is null ? Ast.Expr.Unit.Default : ToAstExpr(db, block.Value)),
            CallExpressionSyntax call => new Ast.Expr.Call(
                SyntaxNode: call,
                Called: ToAstExpr(db, call.Function),
                Args: call.ArgumentList.Value.Elements.Select(a => ToAstExpr(db, a.Value)).ToImmutableArray()),
            RelationalExpressionSyntax rel => new Ast.Expr.Relational(
                SyntaxNode: rel,
                Left: ToAstExpr(db, rel.Left),
                Comparisons: rel.Comparisons.Select(c => ToAstComparisonElement(db, c)).ToImmutableArray()),
            UnaryExpressionSyntax ury => new Ast.Expr.Unary(
                SyntaxNode: ury,
                Operator: SymbolResolution.GetReferencedSymbol<ISymbol.IFunction>(db, ury),
                Operand: ToAstExpr(db, ury.Operand)),
            BinaryExpressionSyntax bin => ToAstExpr(db, bin),
            LiteralExpressionSyntax lit => ToAstExpr(lit),
            StringExpressionSyntax str => ToAstExpr(db, str),
            // We desugar unit statements into { stmt; }
            StatementExpressionSyntax stmt => new Ast.Expr.Block(
                SyntaxNode: stmt,
                Statements: ImmutableArray.Create(ToAstStmt(db, stmt.Statement)),
                Value: Ast.Expr.Unit.Default),
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        });

    /// <summary>
    /// Builds an <see cref="Ast.LValue"/> from the given <see cref="ExpressionSyntax"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ExpressionSyntax"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="expr"/>.</returns>
    public static Ast.LValue ToAstLValue(QueryDatabase db, ExpressionSyntax expr) => db.GetOrUpdate(
        expr,
        Ast.LValue (expr) => expr switch
        {
            UnexpectedExpressionSyntax u => new Ast.LValue.Unexpected(u),
            NameExpressionSyntax name => new Ast.LValue.Reference(
                SyntaxNode: name,
                Symbol: SymbolResolution.GetReferencedSymbol<ISymbol.IVariable>(db, name)),
            _ => new Ast.LValue.Illegal(
                SyntaxNode: expr,
                Diagnostics: ImmutableArray.Create(Diagnostic.Create(
                    template: DataflowErrors.IllegalLValue,
                    location: expr.Location))),
        });

    private static Ast.CompilationUnit ToAstCompilationUnit(QueryDatabase db, CompilationUnitSyntax cu) => db.GetOrUpdate(
        cu,
        Ast.CompilationUnit (cu) => new(
            SyntaxNode: cu,
            Declarations: cu.Declarations.Select(d => ToAstDecl(db, d)).ToImmutableArray()));

    private static Ast.Expr.Block ToAstFuncBody(QueryDatabase db, FunctionBodySyntax funcBody) => db.GetOrUpdate(
        funcBody,
        Ast.Expr.Block (funcBody) => funcBody switch
        {
            UnexpectedFunctionBodySyntax unexpectedBody => ToAstFuncBody(unexpectedBody),
            BlockFunctionBodySyntax blockBody => ToAstFuncBody(db, blockBody),
            InlineFunctionBodySyntax inlineBody => ToAstFuncBody(db, inlineBody),
            _ => throw new ArgumentOutOfRangeException(nameof(funcBody)),
        });

    private static Ast.Expr.Block ToAstFuncBody(QueryDatabase db, BlockFunctionBodySyntax body)
    {
        var statements = ImmutableArray.CreateBuilder<Ast.Stmt>();
        foreach (var stmt in body.Statements) statements.Add(ToAstStmt(db, stmt));
        // If the return type is Unit, append an implicit return statement
        Debug.Assert(body.Parent is FunctionDeclarationSyntax);
        var definedSymbol = SymbolResolution.GetDefinedSymbolOrNull(db, body.Parent!);
        Debug.Assert(definedSymbol is ISymbol.IFunction);
        var funcSymbol = (ISymbol.IFunction)definedSymbol!;
        if (funcSymbol.ReturnType.Equals(Type.Unit))
        {
            // Unit return-type, implicit return
            statements.Add(new Ast.Stmt.Expr(
                SyntaxNode: null,
                Expression: new Ast.Expr.Return(
                    SyntaxNode: null,
                    Expression: Ast.Expr.Unit.Default)));
        }
        return new(
            SyntaxNode: body,
            Statements: statements.ToImmutable(),
            Value: Ast.Expr.Unit.Default);
    }

    // Desugar here into a simple return statement inside a block
    private static Ast.Expr.Block ToAstFuncBody(QueryDatabase db, InlineFunctionBodySyntax body) => new(
        SyntaxNode: body,
        Statements: ImmutableArray.Create<Ast.Stmt>(
            new Ast.Stmt.Expr(
                SyntaxNode: body,
                Expression: new Ast.Expr.Return(
                    SyntaxNode: body,
                    Expression: ToAstExpr(db, body.Value)))),
        Value: Ast.Expr.Unit.Default);

    private static Ast.Expr.Block ToAstFuncBody(UnexpectedFunctionBodySyntax body) => new(
        SyntaxNode: body,
        Statements: ImmutableArray<Ast.Stmt>.Empty,
        Value: Ast.Expr.Unit.Default);

    private static Ast.ComparisonElement ToAstComparisonElement(QueryDatabase db, ComparisonElementSyntax ce) => db.GetOrUpdate(
        ce,
        Ast.ComparisonElement (ce) => new(
            SyntaxNode: ce,
            Operator: SymbolResolution.GetReferencedSymbol<ISymbol.IFunction>(db, ce),
            Right: ToAstExpr(db, ce.Right)));

    private static Ast.Expr ToAstExpr(QueryDatabase db, StringExpressionSyntax str)
    {
        var builder = ImmutableArray.CreateBuilder<Ast.StringPart>();
        var lastNewline = true;
        foreach (var part in str.Parts)
        {
            switch (part)
            {
            case TextStringPartSyntax content:
            {
                var text = content.Content.ValueText;
                Debug.Assert(text is not null);
                builder.Add(new Ast.StringPart.Content(
                    SyntaxNode: content,
                    Value: text![(lastNewline ? str.Cutoff : 0)..]));
                lastNewline = content.Value.Type == TokenType.StringNewline;
                break;
            }
            case InterpolationStringPartSyntax interpolation:
            {
                builder.Add(new Ast.StringPart.Interpolation(
                    SyntaxNode: interpolation,
                    Expression: ToAstExpr(db, interpolation.Expression)));
                lastNewline = false;
                break;
            }
            case UnexpectedStringPartSyntax unexpected:
            {
                // They are likely stray tokens in a multiline string
                break;
            }
            default:
                throw new InvalidOperationException("unknown string part");
            }
        }
        return new Ast.Expr.String(
            SyntaxNode: str,
            builder.ToImmutableArray());
    }

    private static Ast.Expr ToAstExpr(QueryDatabase db, BinaryExpressionSyntax bin)
    {
        // Binary tree either becomes an assignment or a binary expr
        if (Syntax.TokenTypeExtensions.IsCompoundAssignmentOperator(bin.Operator.Type))
        {
            var left = ToAstLValue(db, bin.Left);
            var right = ToAstExpr(db, bin.Right);
            var @operator = SymbolResolution.GetReferencedSymbol<ISymbol.IFunction>(db, bin);
            return new Ast.Expr.Assign(
                SyntaxNode: bin,
                Target: left,
                CompoundOperator: @operator,
                Value: right);
        }
        else if (bin.Operator.Type == TokenType.Assign)
        {
            var left = ToAstLValue(db, bin.Left);
            var right = ToAstExpr(db, bin.Right);
            return new Ast.Expr.Assign(
                SyntaxNode: bin,
                Target: left,
                CompoundOperator: null,
                Value: right);
        }
        else if (bin.Operator.Type == TokenType.KeywordAnd)
        {
            var left = ToAstExpr(db, bin.Left);
            var right = ToAstExpr(db, bin.Right);
            return new Ast.Expr.And(
                SyntaxNode: bin,
                Left: left,
                Right: right);
        }
        else if (bin.Operator.Type == TokenType.KeywordOr)
        {
            var left = ToAstExpr(db, bin.Left);
            var right = ToAstExpr(db, bin.Right);
            return new Ast.Expr.Or(
                SyntaxNode: bin,
                Left: left,
                Right: right);
        }
        else
        {
            var left = ToAstExpr(db, bin.Left);
            var right = ToAstExpr(db, bin.Right);
            var @operator = SymbolResolution.GetReferencedSymbol<ISymbol.IFunction>(db, bin);
            return new Ast.Expr.Binary(
                SyntaxNode: bin,
                Left: left,
                Operator: @operator,
                Right: right);
        }
    }

    private static Ast.Expr ToAstExpr(LiteralExpressionSyntax lit) => lit.Literal.Type switch
    {
        TokenType.LiteralInteger => new Ast.Expr.Literal(
            SyntaxNode: lit,
            Value: lit.Literal.Value,
            Type: Type.Int32),
        TokenType.LiteralFloat => new Ast.Expr.Literal(
            SyntaxNode: lit,
            Value: lit.Literal.Value,
            // NOTE: There is no agreement currently on float literal type
            Type: Type.Float64),
        TokenType.KeywordTrue => new Ast.Expr.Literal(
            SyntaxNode: lit,
            Value: true,
            Type: Type.Bool),
        TokenType.KeywordFalse => new Ast.Expr.Literal(
            SyntaxNode: lit,
            Value: false,
            Type: Type.Bool),
        _ => throw new NotImplementedException(),
    };
}
