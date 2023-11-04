using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Binding.Tasks;

namespace Draco.Compiler.Internal.Solver.Tasks;

internal sealed class SolverTaskMethodBuilder<T>
{
    public SolverTask<T> Task => this.task;
    private SolverTask<T> task;

    public static SolverTaskMethodBuilder<T> Create() => new();

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine _) => Debug.Fail("Unused");

    public void SetException(Exception exception) => this.Task.Awaiter.SetResult(default, exception);
    public void SetResult(T result) => this.Task.Awaiter.SetResult(result, null);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine => awaiter.OnCompleted(stateMachine.MoveNext);

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine => awaiter.OnCompleted(stateMachine.MoveNext);
}
