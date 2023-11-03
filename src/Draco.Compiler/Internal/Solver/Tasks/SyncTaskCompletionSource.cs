using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Solver.Tasks;
#pragma warning disable IDE0250 // Make struct 'readonly'
public struct SyncTaskCompletionSource<T>
{
    // A default CompletionSourceSyncTask is a valid task that can be awaited.
    internal SyncAwaiter<T> Awaiter { get; }
    public readonly bool IsCompleted => this.Awaiter.IsCompleted;
    public readonly T Result => this.Awaiter.GetResult();
    public readonly SyncAwaiter<T> GetAwaiter() => this.Awaiter;
#pragma warning disable IDE0251 // Make member 'readonly'
    public void SetResult(T result) => this.Awaiter.SetResult(result, null);
    public void SetException(Exception exception) => this.Awaiter.SetResult(default, exception);
#pragma warning restore IDE0251 // Make member 'readonly'
}
