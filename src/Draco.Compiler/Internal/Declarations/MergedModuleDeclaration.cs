using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Declarations;

/// <summary>
/// A module merged across all source files.
/// </summary>
internal sealed class MergedModuleDeclaration : Declaration
{
    public override ImmutableArray<Declaration> Children => this.children ??= this.BuildChildren();
    private ImmutableArray<Declaration>? children;

    public string FullName { get; }

    private readonly ImmutableArray<SingleModuleDeclaration> declarations;

    public MergedModuleDeclaration(string name, string fullName, ImmutableArray<SingleModuleDeclaration> declarations)
        : base(name)
    {
        this.declarations = declarations;
        this.FullName = fullName;
    }

    private ImmutableArray<Declaration> BuildChildren()
    {
        var parentNesting = this.FullName.Count(x => x == '.');
        var children = ImmutableArray.CreateBuilder<Declaration>();
        var submodules = new List<SingleModuleDeclaration>();
        foreach (var singleModule in this.declarations)
        {
            submodules.Add(singleModule);
            if (singleModule.FullName != this.FullName) continue;
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

        var directSubmodulesGrouped = submodules.Where(x => x.FullName.Count(y => y == '.') == parentNesting + 1).GroupBy(m => m.FullName);
        var otherSubmodules = submodules.Where(x => x.FullName.Count(y => y == '.') != parentNesting + 1);
        foreach (var group in directSubmodulesGrouped)
        {
            var groupArray = group.ToImmutableArray();
            children.Add(new MergedModuleDeclaration(groupArray[0].Name, groupArray[0].FullName, groupArray.Concat(otherSubmodules.Where(x => x.FullName.StartsWith(groupArray[0].FullName))).ToImmutableArray()));
        }
        return children.ToImmutable();
    }
}
