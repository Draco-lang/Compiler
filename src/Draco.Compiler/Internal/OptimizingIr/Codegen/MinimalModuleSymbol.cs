using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Codegen;

/// <summary>
/// A minimal implementation of <see cref="ModuleSymbol"/> that only contains a name.
/// Used by <see cref="MinimalAssemblyCodegen"/> to back the root module.
/// </summary>
internal sealed class MinimalModuleSymbol(string name) : ModuleSymbol
{
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
    public override string Name => name;
}
