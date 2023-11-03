using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Solver.Tasks;
[AsyncMethodBuilder(typeof(SyncTaskMethodBuilder<>))]
public struct SyncTask<T>
{
    internal SyncAwaiter<T> Awaiter { get; }
    public readonly bool IsCompleted => this.Awaiter.IsCompleted;
    public readonly T Result => this.Awaiter.GetResult();
    public readonly SyncAwaiter<T> GetAwaiter() => this.Awaiter;
}
