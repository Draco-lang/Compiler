using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr.Passes;

/// <summary>
/// TCO can be performed, when a recursive call is returned right after the call itself, meaning there are no computations
/// in between.
///
/// In general, any case of this pattern:
///
/// proc foo(a1: T1, a2: T2, ...):
///   ...
///   tmp1 = call foo [b1, b2, ...]
///   ret tmp1
///   ...
///   tmp2 = call foo [c1, c2, ...]
///   ret tmp2
///   ...
///
/// Can be replaced with the following:
///
/// proc foo(a1: T1, a2: T2, ...):
/// label start:
///   ; Since arguments are immutable, we have to make them mutable by allocating them locally
///   x1 = alloc T1
///   store x1, a1
///   x2 = alloc T2
///   store x2, a2
///   ...
///   store x1, b1
///   store x2, b2
///   ...
///   jmp start
///   ...
///   store x1, c1
///   store x2, c2
///   ...
///   jmp start
///   ...
///
/// IMPORTANTLY, ALL REFERENCES TO ANY OF THE ARGUMENTS HAVE TO BE REPLACED WITH THE MUTABLE ARGUMENT VARIANTS!
/// </summary>
internal static class TailCallOptimization
{
    public static IOptimizationPass Instance { get; } = OptimizationPass.Procedure(
        filter: HasTailCalls,
        passDelegate: Apply);

    private static bool HasTailCalls(Procedure procedure) => procedure.BasicBlocks.Any(bb => HasTailCall(procedure, bb));

    private static bool Apply(Procedure procedure)
    {
        // Make a new entry block for the store
        var oldEntry = procedure.Entry;
        procedure.BasicBlocks.Insert(0, new());

        // Make each argument mutable by
        //   x1 = alloc T1
        //   store x1, a1
        // pairs
        var writer = procedure.Writer();
        writer.Seek(procedure.Entry, 0);
        var argPointers = new List<Value>();
        foreach (var param in procedure.Parameters)
        {
            var argPtr = writer.Alloc(param.Type);
            writer.Store(argPtr, param);
            argPointers.Add(argPtr);
        }
        writer.Jmp(oldEntry);

        // Next, we can replace each occurrence of
        //   tmp2 = call foo [c1, c2, ...]
        //   ret tmp2
        // with
        //   store x1, c1
        //   store x2, c2
        //   ...
        //   jmp start

        foreach (var block in procedure.BasicBlocks.Skip(1))
        {
            if (!HasTailCall(procedure, block)) continue;

            var callArgs = block.Instructions[^2].GetOperandAt<IList<Value>>(2);
            Debug.Assert(callArgs.Count == argPointers.Count);

            // Remove the last two instructions, which are the call and the return
            block.Instructions.RemoveAt(block.Instructions.Count - 1);
            block.Instructions.RemoveAt(block.Instructions.Count - 1);

            // Seek, write stores and jump
            writer.SeekEnd(block);
            for (var i = 0; i < callArgs.Count; ++i)
            {
                var argPtr = argPointers[i];
                var arg = callArgs[i];
                writer.Store(argPtr, arg);
            }
            writer.Jmp(oldEntry);
        }

        // Finally, replace all references to the function arguments to the mutable variants
        // We skip the first one, that's the initial argument assignment
        foreach (var block in procedure.BasicBlocks.Skip(1))
        {
            for (var i = 0; i < block.Instructions.Count; ++i)
            {
                var instr = block.Instructions[i];
                for (var j = 0; j < instr.OperandCount; ++j)
                {
                    var operand = instr.GetOperandAt<object>(j);
                    if (operand is not Value.Param param) continue;
                    var paramIndex = procedure.Parameters.IndexOf(param);
                    Debug.Assert(paramIndex != -1);

                    // The operand references a parameter
                    // Seek before the instruction and write a load to the appropriate pointer
                    writer.Seek(block, i);
                    var loadedValue = writer.Load(argPointers[paramIndex]);
                    instr.SetOperandAt(j, loadedValue);

                    // The instruction pointer has been offset by one
                    ++i;
                }
            }
        }

        return true;
    }

    private static bool HasTailCall(Procedure procedure, BasicBlock basicBlock)
    {
        if (basicBlock.Instructions.Count < 2) return false;
        return IsTailCall(procedure, basicBlock.Instructions[^2], basicBlock.Instructions[^1]);
    }

    private static bool IsTailCall(Procedure procedure, Instruction first, Instruction second)
    {
        if (first.Kind != InstructionKind.Call || second.Kind != InstructionKind.Ret) return false;
        var target = first.GetOperandAt<Value.Reg>(0);
        var called = first.GetOperandAt<Value>(1);
        var returned = second.GetOperandAt<Value>(0);
        if (called != procedure) return false;
        if (target != returned) return false;
        return true;
    }
}
