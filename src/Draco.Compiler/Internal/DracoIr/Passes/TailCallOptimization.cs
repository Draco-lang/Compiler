using System;
using System.Collections.Generic;
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
/// </summary>
internal static class TailCallOptimization
{
    public static IOptimizationPass Instance { get; } = OptimizationPass.Procedure(
        filter: HasTailCalls,
        passDelegate: Apply);

    private static bool HasTailCalls(Procedure procedure)
    {
        var lastRecursiveCallResult = null as Value;
        foreach (var instr in procedure.Instructions)
        {
            if (instr.Kind == InstructionKind.Call)
            {
                if (instr.GetOperandAt<Value>(1) == procedure)
                {
                    lastRecursiveCallResult = instr.GetOperandAt<Value>(0);
                }
                else
                {
                    lastRecursiveCallResult = null;
                }
            }
            else if (instr.Kind == InstructionKind.Ret)
            {
                var returned = instr.GetOperandAt<Value>(0);
                if (returned == lastRecursiveCallResult)
                {
                    return true;
                }
                else
                {
                    lastRecursiveCallResult = null;
                }
            }
            else
            {
                lastRecursiveCallResult = null;
            }
        }
        return false;
    }

    private static bool Apply(Procedure procedure)
    {
        throw new NotImplementedException();
    }
}
