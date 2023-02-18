using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// A module merged across all source files.
/// </summary>
internal sealed class MergedModuleDeclaration : Declaration
{
    public override ImmutableArray<Declaration> Children => this.children ??= this.BuildChildren();
    private ImmutableArray<Declaration>? children;

    private readonly ImmutableArray<SingleModuleDeclaration> declarations;

    public MergedModuleDeclaration(ImmutableArray<SingleModuleDeclaration> declarations)
        : base(declarations.FirstOrDefault()?.Name ?? string.Empty)
    {
        this.declarations = declarations;
    }

    private ImmutableArray<Declaration> BuildChildren()
    {
        var children = ImmutableArray.CreateBuilder<Declaration>();
        var submodules = new List<SingleModuleDeclaration>();
        foreach (var singleModule in this.declarations)
        {
            // singleModule is a piece of this module, we need to go through each element of that
            foreach (var declaration in singleModule.Children)
            {
                if (declaration is SingleModuleDeclaration module)
                {
                    submodules.Add(module);
                }
                else
                {
                    children.Add(declaration);
                }
            }
        }
        // We need to merge submodules by name
        var submodulesGrouped = submodules.GroupBy(m => m.Name);
        // And add them as merged modules
        foreach (var group in submodulesGrouped) children.Add(new MergedModuleDeclaration(group.ToImmutableArray()));
        return children.ToImmutable();
    }
}
