using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Query.Tasks;

/// <summary>
/// Caches async state machines and related functionality.
/// </summary>
/// <typeparam name="TAsm">The exact async state machine type.</typeparam>
/// <typeparam name="TBuilder">The builder type.</typeparam>
internal static class AsmCache<TAsm, TBuilder>
    where TAsm : IAsyncStateMachine
{
    private sealed class EqualityComparer : IEqualityComparer<TAsm>
    {
        private readonly AsmInterface<TAsm, TBuilder>.EqualsDelegate equals;
        private readonly AsmInterface<TAsm, TBuilder>.GetHashCodeDelegate getHashCode;

        public EqualityComparer(
            AsmInterface<TAsm, TBuilder>.EqualsDelegate equals,
            AsmInterface<TAsm, TBuilder>.GetHashCodeDelegate getHashCode)
        {
            this.equals = equals;
            this.getHashCode = getHashCode;
        }

        public bool Equals(TAsm? x, TAsm? y) => this.equals(ref x!, ref y!);
        public int GetHashCode([DisallowNull] TAsm obj) => this.getHashCode(ref obj);
    }

    public static readonly AsmInterface<TAsm, TBuilder> Interface;
    public static readonly ConcurrentDictionary<TAsm, QueryIdentifier> Identities;

    static AsmCache()
    {
        Interface = AsmInterface<TAsm, TBuilder>.Create();
        var comparer = new EqualityComparer(Interface.Equals, Interface.GetHashCode);
        Identities = new(comparer);
    }
}
