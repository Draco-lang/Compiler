using System.Collections.Immutable;

namespace Draco.Compiler.Api;

/// <summary>
/// Represents global import data that can be fed to a <see cref="Compilation"/>.
/// </summary>
/// <param name="ModuleImports">All modules that should be implicitly imported.</param>
/// <param name="ImportAliases">All symbol paths that should be implicitly aliased to a given name in global namespace.</param>
public readonly record struct GlobalImports(
    ImmutableArray<string> ModuleImports,
    ImmutableDictionary<string, string> ImportAliases)
{
    /// <summary>
    /// True, if this is a default or empty structure.
    /// </summary>
    public bool IsDefault => this.ModuleImports.IsDefaultOrEmpty
                          && (this.ImportAliases is null || this.ImportAliases.IsEmpty);
}
