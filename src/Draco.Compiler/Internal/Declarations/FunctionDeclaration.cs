using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// A free-function declaration.
/// </summary>
internal sealed class FunctionDeclaration : Declaration
{
    /// <summary>
    /// The syntax of the declaration.
    /// </summary>
    public FunctionDeclarationSyntax Syntax { get; }

    public override ImmutableArray<Declaration> Children => ImmutableArray<Declaration>.Empty;

    public override IEnumerable<SyntaxNode> DeclaringSyntaxes
    {
        get
        {
            yield return this.Syntax;
        }
    }

    public FunctionDeclaration(FunctionDeclarationSyntax syntax)
        : base(syntax.Name.Text)
    {
        this.Syntax = syntax;
    }
}
