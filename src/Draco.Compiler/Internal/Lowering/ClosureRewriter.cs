using System.Collections.Immutable;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Lowering;

/// <summary>
/// Rewrites local functions into separate closures.
/// </summary>
internal sealed class ClosureRewriter : BoundTreeRewriter
{
    public readonly record struct RewriteResult(BoundNode Body, ImmutableArray<FunctionSymbol> LocalFunctions);

    public static RewriteResult Rewrite(BoundNode body)
    {
        var rewriter = new ClosureRewriter();
        body = body.Accept(rewriter);
        return new(Body: body, LocalFunctions: rewriter.localFunctions.ToImmutable());
    }

    // NOTE: Currently we don't support closures, we only collect out local functions
    // and remove them from the function bodies

    private readonly ImmutableArray<FunctionSymbol>.Builder localFunctions = ImmutableArray.CreateBuilder<FunctionSymbol>();

    private ClosureRewriter()
    {
    }

    // We register the local
    public override BoundNode VisitLocalFunction(BoundLocalFunction node)
    {
        this.localFunctions.Add(node.Symbol);
        return base.VisitLocalFunction(node);
    }

    // In blocks we keep the non-local-function nodes
    public override BoundNode VisitBlockExpression(BoundBlockExpression node)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();
        foreach (var statement in node.Statements)
        {
            var desugared = (BoundStatement)this.VisitStatement(statement);
            if (desugared is BoundLocalFunction) continue;

            statements.Add(desugared);
        }
        var value = (BoundExpression)this.VisitExpression(node.Value);

        return BlockExpression(
            locals: node.Locals,
            statements: statements.ToImmutable(),
            value: value);
    }
}
