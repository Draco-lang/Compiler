using System;
using System.Linq;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Api.Syntax;
using System.Collections.Immutable;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Computations for building the AST.
/// </summary>
internal static class AstBuilder
{
    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="ast">The <see cref="ParseNode"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="ast"/>.</returns>
    public static Ast ToAst(QueryDatabase db, ParseNode ast) => ast switch
    {
        ParseNode.CompilationUnit cu => ToAst(db, cu),
        ParseNode.Decl decl => ToAst(db, decl),
        ParseNode.Stmt stmt => ToAst(db, stmt),
        ParseNode.Expr expr => ToAst(db, expr),
        _ => throw new ArgumentOutOfRangeException(nameof(ast)),
    };

    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="decl">The <see cref="ParseNode"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="decl"/>.</returns>
    public static Ast.Decl ToAst(QueryDatabase db, ParseNode.Decl decl)
    {
        // Get all the doc commemts above the declarationh
        var trivia = decl.Tokens.FirstOrDefault() is not null ?
            decl.Tokens.FirstOrDefault()!.LeadingTrivia.Where(x => x.Type == TriviaType.DocumentationComment) :
            null;
        // Concatenate the text of all the doc comments
        var documentation = trivia is not null ? string.Join(Environment.NewLine, trivia.Select(x => x.Text.TrimStart('/'))) : null;
        return db.GetOrUpdate(
        decl,
        Ast.Decl (decl) => decl switch
        {
            // TODO: Eliminate the ?? pattern everywhere by making the API use optional
            // TODO: Eliminate the null ? null : ... pattern everywhere by making the API use optional

            ParseNode.Decl.Func func => new Ast.Decl.Func(
                ParseNode: func,
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.IFunction>(db, func),
                Documentation: documentation,
                Body: ToAst(db, func.Body)),
            ParseNode.Decl.Label label => new Ast.Decl.Label(
                ParseNode: label,
                LabelSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.ILabel>(db, label)),
            ParseNode.Decl.Variable var => new Ast.Decl.Variable(
                ParseNode: var,
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.IVariable>(db, var),
                Documentation: documentation,
                Value: var.Initializer is null ? null : ToAst(db, var.Initializer.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(decl)),
        });
    }

    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="stmt">The <see cref="ParseNode"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="stmt"/>.</returns>
    public static Ast.Stmt ToAst(QueryDatabase db, ParseNode.Stmt stmt) => db.GetOrUpdate(
        stmt,
        Ast.Stmt (stmt) => stmt switch
        {
            ParseNode.Stmt.Decl d => new Ast.Stmt.Decl(
                ParseNode: d,
                Declaration: ToAst(db, d.Declaration)),
            ParseNode.Stmt.Expr expr => new Ast.Stmt.Expr(
                ParseNode: expr,
                Expression: ToAst(db, expr.Expression)),
            _ => throw new ArgumentOutOfRangeException(nameof(stmt)),
        });

    /// <summary>
    /// Builds an <see cref="Ast"/> from the given <see cref="ParseNode"/>.
    /// </summary>
    /// <param name="db">The <see cref="QueryDatabase"/> for the computation.</param>
    /// <param name="expr">The <see cref="ParseNode"/> to construct the <see cref="Ast"/> from.</param>
    /// <returns>The <see cref="Ast"/> form of <paramref name="expr"/>.</returns>
    public static Ast.Expr ToAst(QueryDatabase db, ParseNode.Expr expr) => db.GetOrUpdate(
        expr,
        Ast.Expr (expr) => expr switch
        {
            ParseNode.Expr.Return ret => new Ast.Expr.Return(
                ParseNode: ret,
                Expression: ret.Expression is null ? Ast.Expr.Unit.Default : ToAst(db, ret.Expression)),
            ParseNode.Expr.Name name => new Ast.Expr.Reference(
                ParseNode: name,
                Symbol: SymbolResolution.GetReferencedSymbolExpected<ISymbol.ITyped>(db, name)),
            ParseNode.Expr.If @if => new Ast.Expr.If(
                ParseNode: @if,
                Condition: ToAst(db, @if.Condition.Value),
                Then: ToAst(db, @if.Then),
                Else: @if.Else is null ? Ast.Expr.Unit.Default : ToAst(db, @if.Else.Expression)),
            ParseNode.Expr.While @while => new Ast.Expr.While(
                ParseNode: @while,
                Condition: ToAst(db, @while.Condition.Value),
                Expression: ToAst(db, @while.Expression)),
            ParseNode.Expr.Block block => new Ast.Expr.Block(
                ParseNode: block,
                Statements: block.Enclosed.Value.Statements.Select(s => ToAst(db, s)).ToImmutableArray(),
                Value: block.Enclosed.Value.Value is null ? Ast.Expr.Unit.Default : ToAst(db, block.Enclosed.Value.Value)),
            ParseNode.Expr.Call call => new Ast.Expr.Call(
                ParseNode: call,
                Called: ToAst(db, call.Called),
                Args: call.Args.Value.Elements.Select(a => ToAst(db, a.Value)).ToImmutableArray()),
            ParseNode.Expr.Relational rel => new Ast.Expr.Relational(
                ParseNode: rel,
                Left: ToAst(db, rel.Left),
                Comparisons: rel.Comparisons.Select(c => ToAst(db, c)).ToImmutableArray()),
            ParseNode.Expr.Unary ury => new Ast.Expr.Unary(
                ParseNode: ury,
                Operator: SymbolResolution.GetReferencedSymbolExpected<ISymbol.IFunction>(db, ury),
                Operand: ToAst(db, ury.Operand)),
            ParseNode.Expr.Binary bin => ToAst(db, bin),
            ParseNode.Expr.Literal lit => ToAst(lit),
            ParseNode.Expr.String str => ToAst(db, str),
            // We desugar unit statements into { stmt; }
            ParseNode.Expr.UnitStmt stmt => new Ast.Expr.Block(
                ParseNode: stmt,
                Statements: ImmutableArray.Create(ToAst(db, stmt.Statement)),
                Value: Ast.Expr.Unit.Default),
            _ => throw new ArgumentOutOfRangeException(nameof(expr)),
        });

    private static Ast.CompilationUnit ToAst(QueryDatabase db, ParseNode.CompilationUnit cu) => db.GetOrUpdate(
        cu,
        Ast.CompilationUnit (cu) => new(
            ParseNode: cu,
            Declarations: cu.Declarations.Select(d => ToAst(db, d)).ToImmutableArray()));

    private static Ast.Expr.Block ToAst(QueryDatabase db, ParseNode.FuncBody funcBody) => db.GetOrUpdate(
        funcBody,
        Ast.Expr.Block (funcBody) => funcBody switch
        {
            ParseNode.FuncBody.BlockBody blockBody => (Ast.Expr.Block)ToAst(db, blockBody.Block),
            // Desugar here into a simple return statement inside a block
            ParseNode.FuncBody.InlineBody inlineBody => new(
                ParseNode: inlineBody,
                Statements: ImmutableArray.Create<Ast.Stmt>(
                    new Ast.Stmt.Expr(
                        ParseNode: inlineBody,
                        Expression: new Ast.Expr.Return(
                            ParseNode: inlineBody,
                            Expression: ToAst(db, inlineBody.Expression)))),
                Value: Ast.Expr.Unit.Default),
            _ => throw new ArgumentOutOfRangeException(nameof(funcBody)),
        });

    private static Ast.ComparisonElement ToAst(QueryDatabase db, ParseNode.ComparisonElement ce) => db.GetOrUpdate(
        ce,
        Ast.ComparisonElement (ce) => new(
            ParseNode: ce,
            Operator: SymbolResolution.GetReferencedSymbolExpected<ISymbol.IFunction>(db, ce),
            Right: ToAst(db, ce.Right)));

    private static Ast.Expr ToAst(QueryDatabase db, ParseNode.Expr.String str)
    {
        // TODO: Maybe move the cutoff/first-in-line logic into the parser itself?
        // It's a bit misleading to have a nonzero cutoff for parts not at the start of the line
        var builder = ImmutableArray.CreateBuilder<Ast.StringPart>();
        var firstInLine = true;
        foreach (var part in str.Parts)
        {
            builder.Add(part switch
            {
                ParseNode.StringPart.Content content => new Ast.StringPart.Content(
                    ParseNode: content,
                    Value: content.Value.ValueText ?? throw new InvalidOperationException(),
                    Cutoff: firstInLine ? content.Cutoff : 0),
                ParseNode.StringPart.Interpolation interpolation => new Ast.StringPart.Interpolation(
                    ParseNode: interpolation,
                    Expression: ToAst(db, interpolation.Expression)),
                _ => throw new InvalidOperationException(),
            });
            firstInLine = part is ParseNode.StringPart.Content c && c.Value.Type == TokenType.StringNewline;
        }
        return new Ast.Expr.String(
            ParseNode: str,
            builder.ToImmutableArray());
    }

    private static Ast.Expr ToAst(QueryDatabase db, ParseNode.Expr.Binary bin)
    {
        var left = ToAst(db, bin.Left);
        var right = ToAst(db, bin.Right);

        // Binary tree either becomes an assignment or a binary expr
        if (Syntax.TokenTypeExtensions.IsCompoundAssignmentOperator(bin.Operator.Type))
        {
            var @operator = SymbolResolution.GetReferencedSymbolExpected<ISymbol.IFunction>(db, bin);
            return new Ast.Expr.Assign(
                ParseNode: bin,
                Target: left,
                CompoundOperator: @operator,
                Value: right);
        }
        else if (bin.Operator.Type == TokenType.Assign)
        {
            return new Ast.Expr.Assign(
                ParseNode: bin,
                Target: left,
                CompoundOperator: null,
                Value: right);
        }
        else if (bin.Operator.Type == TokenType.KeywordAnd)
        {
            return new Ast.Expr.And(
                ParseNode: bin,
                Left: left,
                Right: right);
        }
        else if (bin.Operator.Type == TokenType.KeywordOr)
        {
            return new Ast.Expr.Or(
                ParseNode: bin,
                Left: left,
                Right: right);
        }
        else
        {
            var @operator = SymbolResolution.GetReferencedSymbolExpected<ISymbol.IFunction>(db, bin);
            return new Ast.Expr.Binary(
                ParseNode: bin,
                Left: left,
                Operator: @operator,
                Right: right);
        }
    }

    private static Ast.Expr ToAst(ParseNode.Expr.Literal lit) => lit.Value.Type switch
    {
        TokenType.LiteralInteger => new Ast.Expr.Literal(
            ParseNode: lit,
            Value: lit.Value.Value,
            Type: Type.Int32),
        TokenType.KeywordTrue => new Ast.Expr.Literal(
            ParseNode: lit,
            Value: true,
            Type: Type.Bool),
        TokenType.KeywordFalse => new Ast.Expr.Literal(
            ParseNode: lit,
            Value: false,
            Type: Type.Bool),
        _ => throw new NotImplementedException(),
    };
}
