using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Query;

// Documentation about Task-Types: https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md

[StructLayout(LayoutKind.Auto)]
public struct QueryValueTaskMethodBuilder<T>
{
    private record struct CachedValue(T Value, int Identity);

    private static readonly ConcurrentDictionary<IAsyncStateMachine, CachedValue> cachedResults = new(AsmComparer.Instance);

    public static QueryValueTaskMethodBuilder<T> Create() => new();

    public QueryValueTask<T> Task => this.stateMachine is null
        ? new(this.result!, this.identity)
        : new(this.valueTaskBuilder.Task, this.identity);

    private static int identityCounter = 0;

    private int identity = -1;
    private AsyncValueTaskMethodBuilder<T> valueTaskBuilder;
    private IAsyncStateMachine? stateMachine = null;
    private bool hasResult = false;
    private T? result = default;

    public QueryValueTaskMethodBuilder()
    {
        this.valueTaskBuilder = AsyncValueTaskMethodBuilder<T>.Create(); // this is in fact "default"
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        // We will compare the current state machine to the one stored.
        // The state machine contains the full state of the async method.
        // In this codepath the stateMachine did not ran so the parameters captured,
        // and the intermediates values have all a default value.

        // Now we can store a copy of this state machine, and the result it produce.

        if (cachedResults.TryGetValue(stateMachine, out var val))
        {
            // In this codepath, we found a cached result.
            // When this.result is set we bypass any async code and expose a completed query with the result.
            this.hasResult = true;
            this.result = val.Value;
            this.identity = val.Identity;
            return;
        }

        // There was no cached result.
        this.identity = Interlocked.Increment(ref identityCounter);

        // In debug, the state machine is a class, so must be cloned.
        // If not, the function execution would change the state.
        this.stateMachine = AsmCloner.Clone(stateMachine);

        // We then delegate the real work.
        this.valueTaskBuilder.Start(ref stateMachine);
    }

    // this method is deprecated and shouldn't be called. The implemention simply Debug.Fail.
    public void SetStateMachine(IAsyncStateMachine stateMachine) => this.valueTaskBuilder.SetStateMachine(stateMachine);
    public void SetException(Exception exception) => this.valueTaskBuilder.SetException(exception);

    public void SetResult(T result)
    {
        // We save the result to the cache for future calls.
        cachedResults[this.stateMachine!] = new(result, this.identity);
        this.valueTaskBuilder.SetResult(result);
    }

    private void CheckIsDependentQuery<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : INotifyCompletion
    {
        if (awaiter is IIdentifiableQueryAwaiter query)
        {
            Console.WriteLine($"Query {this.identity} depends on query {query.Identity}");
        }
    }

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        this.CheckIsDependentQuery(ref awaiter);
        if (this.hasResult)
        {
            stateMachine.MoveNext();
            return;
        }
        this.valueTaskBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        this.CheckIsDependentQuery(ref awaiter);
        if (this.hasResult)
        {
            stateMachine.MoveNext();
            return;
        }
        this.valueTaskBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
    }
}
