using System;
using System.Linq;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Api.Syntax;
using System.Collections.Immutable;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;
using System.Diagnostics;

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
    public static Ast.Decl ToAst(QueryDatabase db, ParseNode.Decl decl) => db.GetOrUpdate(
        decl,
        Ast.Decl (decl) => decl switch
        {
            // TODO: Eliminate the ?? pattern everywhere by making the API use optional
            // TODO: Eliminate the null ? null : ... pattern everywhere by making the API use optional

            ParseNode.Decl.Func func => new Ast.Decl.Func(
                ParseNode: func,
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.IFunction>(db, func),
                Body: ToAst(db, func.Body)),
            ParseNode.Decl.Label label => new Ast.Decl.Label(
                ParseNode: label,
                LabelSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.ILabel>(db, label)),
            ParseNode.Decl.Variable var => new Ast.Decl.Variable(
                ParseNode: var,
                DeclarationSymbol: SymbolResolution.GetDefinedSymbolExpected<ISymbol.IVariable>(db, var),
                Value: var.Initializer is null ? null : ToAst(db, var.Initializer.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(decl)),
        });

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
            ParseNode.Expr.Grouping g => ToAst(db, g.Expression.Value),
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
            ParseNode.FuncBody.BlockBody blockBody => ToAst(db, blockBody),
            ParseNode.FuncBody.InlineBody inlineBody => ToAst(db, inlineBody),
            _ => throw new ArgumentOutOfRangeException(nameof(funcBody)),
        });

    private static Ast.Expr.Block ToAst(QueryDatabase db, ParseNode.FuncBody.BlockBody body)
    {
        var block = body.Block.Enclosed.Value;
        var statements = ImmutableArray.CreateBuilder<Ast.Stmt>();
        foreach (var stmt in block.Statements) statements.Add(ToAst(db, stmt));
        if (block.Value is not null)
        {
            // Desugar it into a statement
            statements.Add(new Ast.Stmt.Expr(
                ParseNode: block.Value,
                Expression: ToAst(db, block.Value)));
        }
        // If the return type is Unit, append an implicit return statement
        Debug.Assert(body.Parent is ParseNode.Decl.Func);
        var definedSymbol = SymbolResolution.GetDefinedSymbolOrNull(db, body.Parent!);
        Debug.Assert(definedSymbol is ISymbol.IFunction);
        var funcSymbol = (ISymbol.IFunction)definedSymbol!;
        if (funcSymbol.ReturnType.Equals(Type.Unit))
        {
            // Unit return-type, implicit return
            statements.Add(new Ast.Stmt.Expr(
                ParseNode: null,
                Expression: new Ast.Expr.Return(
                    ParseNode: null,
                    Expression: Ast.Expr.Unit.Default)));
        }
        return new(
            ParseNode: body,
            Statements: statements.ToImmutable(),
            Value: Ast.Expr.Unit.Default);
    }

    // Desugar here into a simple return statement inside a block
    private static Ast.Expr.Block ToAst(QueryDatabase db, ParseNode.FuncBody.InlineBody body) => new(
        ParseNode: body,
        Statements: ImmutableArray.Create<Ast.Stmt>(
            new Ast.Stmt.Expr(
                ParseNode: body,
                Expression: new Ast.Expr.Return(
                    ParseNode: body,
                    Expression: ToAst(db, body.Expression)))),
        Value: Ast.Expr.Unit.Default);

    private static Ast.ComparisonElement ToAst(QueryDatabase db, ParseNode.ComparisonElement ce) => db.GetOrUpdate(
        ce,
        Ast.ComparisonElement (ce) => new(
            ParseNode: ce,
            Operator: SymbolResolution.GetReferencedSymbolExpected<ISymbol.IFunction>(db, ce),
            Right: ToAst(db, ce.Right)));

    private static Ast.Expr ToAst(QueryDatabase db, ParseNode.Expr.String str)
    {
        var builder = ImmutableArray.CreateBuilder<Ast.StringPart>();
        var lastNewline = true;
        foreach (var part in str.Parts)
        {
            if (part is ParseNode.StringPart.Content content)
            {
                var text = content.Value.ValueText;
                Debug.Assert(text is not null);
                builder.Add(new Ast.StringPart.Content(
                    ParseNode: content,
                    Value: text![(lastNewline ? str.Cutoff : 0)..]));
                lastNewline = content.Value.Type == TokenType.StringNewline;
            }
            else
            {
                var interpolation = (ParseNode.StringPart.Interpolation)part;
                builder.Add(new Ast.StringPart.Interpolation(
                    ParseNode: interpolation,
                    Expression: ToAst(db, interpolation.Expression)));
                lastNewline = false;
            }
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
