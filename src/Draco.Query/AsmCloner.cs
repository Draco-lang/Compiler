using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query;

/// <summary>
/// Utility for cloning async state machines.
/// </summary>
internal static class AsmCloner
{
    private static MethodInfo memberwiseCloneMethod;

    static AsmCloner()
    {
        memberwiseCloneMethod = typeof(object).GetMethod(nameof(MemberwiseClone))!;
    }

    public static TAsm Clone<TAsm>(TAsm asm)
#if DEBUG
        where TAsm : class, IAsyncStateMachine
#else
        where TAsm : IAsyncStateMachine
#endif
    {
#if DEBUG
        // In debug it's a class, we need to memberwise-clone
        return Unsafe.As<TAsm>(memberwiseCloneMethod.Invoke(asm, null))!;
#else
        // In release it's a struct, we can just return it
        return asm;
#endif
    }
}
