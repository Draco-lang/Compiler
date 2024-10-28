using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// A class declaration.
/// </summary>
internal sealed class ClassDeclaration(ClassDeclarationSyntax syntax) : Declaration(syntax.Name.Text)
{
    /// <summary>
    /// The syntax of the declaration.
    /// </summary>
    public ClassDeclarationSyntax Syntax { get; } = syntax;

    public override ImmutableArray<Declaration> Children =>
        InterlockedUtils.InitializeDefault(ref this.children, this.BuildChildren);
    private ImmutableArray<Declaration> children;

    public override IEnumerable<SyntaxNode> DeclaringSyntaxes => [this.Syntax];

    private ImmutableArray<Declaration> BuildChildren()
    {
        if (this.Syntax.Body is not BlockClassBodySyntax block) return ImmutableArray<Declaration>.Empty;

        return block.Declarations.Select(this.BuildChild).OfType<Declaration>().ToImmutableArray();
    }

    // TODO: More entries to handle
    private Declaration? BuildChild(SyntaxNode node) => node switch
    {
        // NOTE: We ignore import declarations in the declaration tree, unlike Roslyn
        // We handle import declarations during constructing the binders
        // Since we allow for imports in local scopes too, this is the most sensible choice
        ImportDeclarationSyntax => null,
        UnexpectedDeclarationSyntax => null,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
