using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// Represents a portion of a module that was read up from a single file.
/// </summary>
internal sealed class SingleModuleDeclaration : Declaration
{
    /// <summary>
    /// The syntax node of this module portion.
    /// </summary>
    public CompilationUnitSyntax Syntax { get; }

    public override ImmutableArray<Declaration> Children => this.children ??= this.BuildChildren();
    private ImmutableArray<Declaration>? children;

    public SingleModuleDeclaration(string name, CompilationUnitSyntax syntax)
        : base(name)
    {
        this.Syntax = syntax;
    }

    private ImmutableArray<Declaration> BuildChildren() =>
        this.Syntax.Children.Select(BuildChild).OfType<Declaration>().ToImmutableArray();

    private static Declaration? BuildChild(SyntaxNode node) => node switch
    {
        VariableDeclarationSyntax var => new GlobalDeclaration(var),
        FunctionDeclarationSyntax func => new FunctionDeclaration(func),
        UnexpectedDeclarationSyntax => null,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
