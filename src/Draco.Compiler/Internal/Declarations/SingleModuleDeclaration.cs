using System;
using System.Collections.Immutable;
using System.Linq;
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
    public ContainerSyntax Syntax { get; }

    /// <summary>
    /// The path of this module, including the root module.
    /// </summary>
    public SplitPath Path { get; }

    public override ImmutableArray<Declaration> Children =>
        InterlockedUtils.InitializeDefault(ref this.children, this.BuildChildren);
    private ImmutableArray<Declaration> children;

    public SingleModuleDeclaration(string name, SplitPath path, ContainerSyntax syntax)
        : base(name)
    {
        this.Syntax = syntax;
        this.Path = path;
    }

    private ImmutableArray<Declaration> BuildChildren() =>
        this.Syntax.Declarations.Select(this.BuildChild).OfType<Declaration>().ToImmutableArray();

    private Declaration? BuildChild(SyntaxNode node) => node switch
    {
        // NOTE: We ignore import declarations in the declaration tree, unlike Roslyn
        // We handle import declarations during constructing the binders
        // Since we allow for imports in local scopes too, this is the most sensible choice
        ImportDeclarationSyntax => null,
        VariableDeclarationSyntax var => new GlobalDeclaration(var),
        FunctionDeclarationSyntax func => new FunctionDeclaration(func),
        ModuleDeclarationSyntax module => new SingleModuleDeclaration(module.Name.Text, this.Path.Append(module.Name.Text), module),
        UnexpectedDeclarationSyntax => null,
        _ => throw new ArgumentOutOfRangeException(nameof(node)),
    };
}
