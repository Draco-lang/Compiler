using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

internal sealed class ReplaceRewriter : SyntaxRewriter
{
    private readonly ImmutableDictionary<SyntaxNode, SyntaxNode> replacements;

    public ReplaceRewriter(ImmutableDictionary<SyntaxNode, SyntaxNode> replacements)
    {
        this.replacements = replacements;
    }

    public override SyntaxNode VisitStatement(StatementSyntax node) => this.ReplaceOrKeep(node);
    public override SyntaxNode VisitDeclaration(DeclarationSyntax node) => this.ReplaceOrKeep(node);
    public override SyntaxNode VisitExpression(ExpressionSyntax node) => this.ReplaceOrKeep(node);
    public override SyntaxNode VisitType(TypeSyntax node) => this.ReplaceOrKeep(node);

    private SyntaxNode ReplaceOrKeep(SyntaxNode node) => this.replacements.TryGetValue(node, out var replacement)
        ? replacement
        : node;
}
