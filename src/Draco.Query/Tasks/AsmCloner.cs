using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query.Tasks;

/// <summary>
/// Utility for cloning async state machines.
/// </summary>
internal static class AsmCloner
{
    private static readonly MethodInfo memberwiseCloneMethod;

    static AsmCloner()
    {
        memberwiseCloneMethod = typeof(object).GetMethod(
            nameof(MemberwiseClone),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    /// <summary>
    /// Shallow clones the given async state machine.
    /// </summary>
    /// <typeparam name="TAsm">The exact type of the async state machine.</typeparam>
    /// <param name="asm">The async state machine to clone.</param>
    /// <returns>A shallow clone of <paramref name="asm"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TAsm Clone<TAsm>(TAsm asm)
        where TAsm : IAsyncStateMachine => typeof(TAsm).IsValueType
        ? asm
        : (TAsm)memberwiseCloneMethod.Invoke(asm, null)!;
}
