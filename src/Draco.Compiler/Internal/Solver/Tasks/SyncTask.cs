using System.Runtime.CompilerServices;

namespace Draco.Compiler.Internal.Solver.Tasks;

[AsyncMethodBuilder(typeof(SyncTaskMethodBuilder<>))]
public struct SyncTask<T>
{
    internal SyncAwaiter<T> Awaiter { get; }
    public readonly bool IsCompleted => this.Awaiter.IsCompleted;
    public readonly T Result => this.Awaiter.GetResult();
    public readonly SyncAwaiter<T> GetAwaiter() => this.Awaiter;
}
