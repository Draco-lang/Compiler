using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IAssembly"/> implementation.
/// </summary>
internal sealed class Assembly : IAssembly
{
    private Procedure? entryPoint;
    private readonly Module rootModule;

    public string Name { get; set; } = "output";
    public Procedure? EntryPoint
    {
        get => this.entryPoint;
        set
        {
            if (value is null)
            {
                this.entryPoint = null;
                return;
            }
            if (!ReferenceEquals(this, value.Assembly))
            {
                throw new System.InvalidOperationException("entry point must be part of the assembly");
            }
            this.entryPoint = value;
        }
    }
    IProcedure? IAssembly.EntryPoint => this.EntryPoint;

    public Module RootModule => this.rootModule;
    IModule IAssembly.RootModule => this.RootModule;

    public ImmutableArray<IProcedure> GetAllProcedures() => this.rootModule.GetProcedures();

    public Assembly(ModuleSymbol module)
    {
        this.rootModule = new Module(module, this, null);
    }

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
