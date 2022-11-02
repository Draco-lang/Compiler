using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Draco.Query.Tasks;

// Documentation about Task-Types: https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md

[StructLayout(LayoutKind.Auto)]
public struct QueryValueTaskMethodBuilder<T>
{
    private static readonly ConcurrentDictionary<IAsyncStateMachine, QueryIdentifier> identityCache = new(AsmComparer.Instance);
    private static readonly ConcurrentDictionary<QueryIdentifier, Func<QueryValueTask<T>>> startCloneCache = new();

    public static QueryValueTaskMethodBuilder<T> Create() => new();

    public QueryValueTask<T> Task => this.stateMachine is null
        ? new(this.result!, this.identity)
        : new(this.valueTaskBuilder.Task, this.identity);

    private QueryIdentifier identity = QueryIdentifier.Invalid;
    private AsyncValueTaskMethodBuilder<T> valueTaskBuilder;
    private IAsyncStateMachine? stateMachine = null;
    private bool hasResult = false;
    private T? result = default;

    public QueryValueTaskMethodBuilder()
    {
        this.valueTaskBuilder = AsyncValueTaskMethodBuilder<T>.Create(); // this is in fact "default"
    }

    private static TStateMachine CloneAsm<TStateMachine>(TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine =>
        AsmUtils<TStateMachine, QueryValueTaskMethodBuilder<T>>.Clone(stateMachine);

    private static ref QueryValueTaskMethodBuilder<T> GetBuilder<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine =>
        ref AsmUtils<TStateMachine, QueryValueTaskMethodBuilder<T>>.GetBuilder(ref stateMachine);

    public static QueryValueTask<T> RunQueryByIdentifier(QueryIdentifier identifier)
    {
        // NOTE: This should never happen
        if (!startCloneCache.TryGetValue(identifier, out var startClone)) throw new InvalidOperationException();
        return startClone();
    }

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine
    {
        // We will compare the current state machine to the one stored
        // The state machine contains the full state of the async method
        // In this codepath the stateMachine did not ran so the parameters captured,
        // and the intermediates values have all a default value

        if (identityCache.TryGetValue(stateMachine, out var oldIdentity))
        {
            // In this codepath, we found a cached identity, meaning this query was ran with the
            // exact parameters before
            // We rewrite our identity to match
            this.identity = oldIdentity;
            if (QueryDatabase.TryGetUpToDateQueryResult<T>(this.identity, out var oldResult))
            {
                // Yes, we found a cached result that is up to date
                // Short-circuit the called query
                this.hasResult = true;
                this.result = oldResult;
                return;
            }
            // No, the query is not up to date, we need to re-run it
            var clonedMachine = CloneAsm(stateMachine);
            this.stateMachine = clonedMachine;
        }
        else
        {
            // There was no cached identity, we create a new one and register it in the system
            this.identity = QueryIdentifier.New;
            QueryDatabase.OnNewQuery<T>(this.identity);

            // In debug, the state machine is a class, so must be cloned
            // If not, the function execution would change the state and the identity cache would
            // get into an inconsistent state
            var clonedMachine = CloneAsm(stateMachine);
            this.stateMachine = clonedMachine;

            // Update caches to include new entry
            identityCache[this.stateMachine] = this.identity;
            startCloneCache[this.identity] = () =>
            {
                var clone2 = CloneAsm(clonedMachine);
                GetBuilder(ref clone2) = Create();
                GetBuilder(ref clone2).Start(ref clone2);
                return GetBuilder(ref clone2).Task;
            };
        }

        // We then delegate the real work, as we have to re-execute the query
        this.valueTaskBuilder.Start(ref stateMachine);
    }

    // This method is deprecated and shouldn't be called
    // The implemention simply Debug.Fail
    public void SetStateMachine(IAsyncStateMachine stateMachine) => this.valueTaskBuilder.SetStateMachine(stateMachine);
    public void SetException(Exception exception) => this.valueTaskBuilder.SetException(exception);

    public void SetResult(T result)
    {
        // We notify the system about the result
        QueryDatabase.OnQueryResult(this.identity, result);
        this.valueTaskBuilder.SetResult(result);
    }

    private void CheckIsDependentQuery<TAwaiter>(ref TAwaiter awaiter)
        where TAwaiter : INotifyCompletion
    {
        if (awaiter is IIdentifiableQueryAwaiter query)
        {
            // Register dependency in the system
            QueryDatabase.OnQueryDependency(dependent: this.identity, dependency: query.Identity);
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
