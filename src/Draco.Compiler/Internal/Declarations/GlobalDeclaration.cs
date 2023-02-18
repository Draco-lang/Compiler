using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// A global variable declaration.
/// </summary>
internal sealed class GlobalDeclaration : Declaration
{
    /// <summary>
    /// The syntax of the declaration.
    /// </summary>
    public VariableDeclarationSyntax Syntax { get; }

    public override ImmutableArray<Declaration> Children => ImmutableArray<Declaration>.Empty;

    public GlobalDeclaration(VariableDeclarationSyntax syntax)
        : base(syntax.Name.Text)
    {
        this.Syntax = syntax;
    }
}
