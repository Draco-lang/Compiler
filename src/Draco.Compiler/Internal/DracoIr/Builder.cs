using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Builds an <see cref="Assembly"/>.
/// </summary>
internal sealed class AssemblyBuilder
{
    /// <summary>
    /// The procedures within this assembly.
    /// </summary>
    public IDictionary<string, ProcBuilder> Procedures { get; set; } = new Dictionary<string, ProcBuilder>();

    /// <summary>
    /// Builds the <see cref="Assembly"/> from this builder.
    /// </summary>
    /// <returns>The built <see cref="Assembly"/>.</returns>
    public Assembly Build() => new(
        Procs: this.Procedures.ToImmutableDictionary(kv => kv.Key, kv => kv.Value.Build()));
}

/// <summary>
/// Builds a <see cref="Proc"/>.
/// </summary>
internal sealed class ProcBuilder
{
    /// <summary>
    /// The name of the procedure.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The parameters for this procedure.
    /// </summary>
    public ImmutableArray<Value.Param>.Builder Params { get; set; } = ImmutableArray.CreateBuilder<Value.Param>();

    /// <summary>
    /// The return type of the procedure.
    /// </summary>
    public Type ReturnType { get; set; } = Type.Void;

    /// <summary>
    /// The instructions within this builder.
    /// </summary>
    public IList<Instr> Instructions { get; set; } = new List<Instr>();

    /// <summary>
    /// Builds the <see cref="Proc"/> from this builder.
    /// </summary>
    /// <returns>The built <see cref="Proc"/>.</returns>
    public Proc Build() => new(
        Name: this.Name,
        Params: this.Params.ToImmutable(),
        ReturnType: this.ReturnType,
        BasicBlocks: this.BuildBasicBlocks());

    private ImmutableArray<BasicBlock> BuildBasicBlocks()
    {
        var result = ImmutableArray.CreateBuilder<BasicBlock>();
        var currentBuffer = ImmutableArray.CreateBuilder<Instr>();
        foreach (var instr in this.Instructions)
        {
            currentBuffer.Add(instr);
            if (instr.IsJump)
            {
                result.Add(new(Instructions: currentBuffer.ToImmutable()));
                currentBuffer.Clear();
            }
        }
        return result.ToImmutable();
    }
}
