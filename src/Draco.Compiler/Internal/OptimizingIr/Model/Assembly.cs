using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IAssembly"/> implementation.
/// </summary>
internal sealed class Assembly(ModuleSymbol module) : IAssembly
{
    private readonly Module rootModule = new(module);

    public string Name { get; set; } = "output";
    public Procedure? EntryPoint { get; set; }
    IProcedure? IAssembly.EntryPoint => this.EntryPoint;

    public Module RootModule => this.rootModule;
    IModule IAssembly.RootModule => this.RootModule;

    public ImmutableArray<IProcedure> GetAllProcedures() => this.rootModule.GetProcedures();

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"assembly {this.Name}");
        if (this.EntryPoint is not null) result.AppendLine($"entry {this.EntryPoint.Name}");
        result.AppendLine();
        result.Append(this.rootModule);
        return result.ToString();
    }
}
