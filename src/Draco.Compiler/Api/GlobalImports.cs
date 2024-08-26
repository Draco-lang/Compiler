using System.Collections.Immutable;

namespace Draco.Compiler.Api;

/// <summary>
/// Represents global import data that can be fed to a <see cref="Compilation"/>.
/// </summary>
/// <param name="ModuleImports">All modules that should be implicitly imported.</param>
/// <param name="ImportAliases">All symbol paths that should be implicitly aliased to a given name in global namespace.</param>
public readonly record struct GlobalImports(
    ImmutableArray<string> ModuleImports,
    ImmutableArray<(string Name, string FullPath)> ImportAliases)
{
    /// <summary>
    /// Combines two global import structures into one.
    /// </summary>
    /// <param name="i1">The first global import structure.</param>
    /// <param name="i2">The second global import structure.</param>
    /// <returns>The combined global import structure.</returns>
    public static GlobalImports Combine(GlobalImports i1, GlobalImports i2) => new(
        i1.ModuleImports.AddRange(i2.ModuleImports),
        i1.ImportAliases.AddRange(i2.ImportAliases));

    /// <summary>
    /// True, if this is a default or empty structure.
    /// </summary>
    public bool IsDefault => this.ModuleImports.IsDefaultOrEmpty
                          && this.ImportAliases.IsDefaultOrEmpty;
}
