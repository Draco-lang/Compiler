using System.Collections.Immutable;
using System.Text;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IAssembly"/> implementation.
/// </summary>
internal sealed class Assembly : IAssembly
{
    private Procedure? entryPoint;
    private Module? rootModule;

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

    public IModule RootModule => this.rootModule ?? throw new System.InvalidOperationException();

    public ImmutableArray<IProcedure> GetAllProcedures() => (this.rootModule ?? throw new System.InvalidOperationException()).GetProcedures();

    public void AddRootModule(Module module) => this.rootModule = module;

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
