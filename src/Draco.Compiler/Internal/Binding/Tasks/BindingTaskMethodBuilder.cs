using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Draco.Compiler.Internal.Binding.Tasks;

internal sealed class BindingTaskMethodBuilder<T>
{
    public BindingTask<T> Task => this.task;
    private BindingTask<T> task = new();

    public static BindingTaskMethodBuilder<T> Create() => new();

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
