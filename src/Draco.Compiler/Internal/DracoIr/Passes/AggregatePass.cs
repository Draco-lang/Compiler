using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr.Passes;

internal sealed class AggregatePass : IInterproceduralPass
{
    public IList<IGlobalPass> GlobalPasses { get; } = new List<IGlobalPass>();
    public IList<IInstructionPass> InstructionPasses { get; } = new List<IInstructionPass>();

    public bool Matches(IReadOnlyAssembly assembly) => true;

    public void Pass(Assembly assembly)
    {
        foreach (var pass in this.InstructionPasses) this.Apply(assembly, pass);
        foreach (var pass in this.GlobalPasses) this.Apply(assembly, pass);
    }

    private void Apply(Assembly assembly, IGlobalPass pass)
    {
        foreach (var procedure in assembly.Procedures.Values) pass.Pass(procedure);
    }

    private void Apply(Assembly assembly, IInstructionPass pass)
    {
        foreach (var procedure in assembly.Procedures.Values)
        {
            foreach (var bb in procedure.BasicBlocks)
            {
                for (var i = 0; i < bb.Instructions.Count; ++i)
                {
                    var instr = bb.Instructions[i];
                    if (!pass.Matches(instr)) continue;
                    bb.Instructions[i] = (Instruction)pass.Pass(instr);
                }
            }
        }
    }
}
