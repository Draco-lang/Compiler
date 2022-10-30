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
        memberwiseCloneMethod = typeof(object).GetMethod(
            nameof(MemberwiseClone),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAsm Clone<TAsm>(TAsm asm)
        where TAsm : IAsyncStateMachine => typeof(TAsm).IsValueType
        ? asm
        : (TAsm)memberwiseCloneMethod.Invoke(asm, null)!;
}
