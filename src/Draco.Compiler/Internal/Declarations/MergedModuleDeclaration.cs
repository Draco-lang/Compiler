using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// A module merged across all source files.
/// </summary>
internal sealed class MergedModuleDeclaration : Declaration
{
    public override ImmutableArray<Declaration> Children =>
        InterlockedUtils.InitializeDefault(ref this.children, this.BuildChildren);
    private ImmutableArray<Declaration> children;

    public override IEnumerable<SyntaxNode> DeclaringSyntaxes => this.declarations
        .SelectMany(d => d.DeclaringSyntaxes);

    /// <summary>
    /// The path of this module, including the root module.
    /// </summary>
    public SplitPath Path { get; }

    public ContainerSyntax? Syntax { get; }

    private readonly ImmutableArray<SingleModuleDeclaration> declarations;

    public MergedModuleDeclaration(string name, SplitPath path, ImmutableArray<SingleModuleDeclaration> declarations, ContainerSyntax? syntax = null)
        : base(name)
    {
        this.declarations = declarations;
        this.Path = path;
        this.Syntax = syntax;
    }

    private ImmutableArray<Declaration> BuildChildren()
    {
        var nesting = this.Path.Length;

        // Collect children here directly
        var children = ImmutableArray.CreateBuilder<Declaration>();
        // Collect submodules under this module
        var submodules = new List<SingleModuleDeclaration>();

        foreach (var singleModule in this.declarations)
        {
            submodules.Add(singleModule);

            // More nested submodule, some other merged module will deal with this
            if (singleModule.Path != this.Path) continue;

            // The paths are the same, unwrap into this module, it contributes to this module
            foreach (var declaration in singleModule.Children)
            {
                if (declaration is SingleModuleDeclaration module)
                {
                    // Yet another submodule
                    submodules.Add(module);
                }
                else
                {
                    // Just a raw child declaration
                    children.Add(declaration);
                }
            }
        }

        // Submodules directly under this module
        var directSubmodules = submodules
            .Where(x => x.Path.Length == nesting + 1)
            .GroupBy(m => m.Path);

        // More nested submodules
        var deeperNestedSubmodules = submodules
            .Where(x => x.Path.Length != nesting + 1)
            .ToList();

        foreach (var group in directSubmodules)
        {
            // We pass the non direct submodules that start with the same name as the grouped module as submodules
            var childDeclarations = group
                .Concat(deeperNestedSubmodules.Where(x => x.Path.StartsWith(group.Key)))
                .ToImmutableArray();

            children.Add(new MergedModuleDeclaration(
                name: group.Key.Last,
                path: group.Key,
                declarations: childDeclarations,
                syntax: group.Count() == 1
                    ? group.First().Syntax
                    : null));
        }
        return children.ToImmutable();
    }
}
