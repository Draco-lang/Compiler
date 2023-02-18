using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public FunctionDeclaration(FunctionDeclarationSyntax syntax)
        : base(syntax.Name.Text)
    {
        this.Syntax = syntax;
    }
}
