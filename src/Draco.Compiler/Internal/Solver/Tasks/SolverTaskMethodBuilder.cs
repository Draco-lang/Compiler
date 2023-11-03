using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Solver.Tasks;
public class SolverTaskMethodBuilder<T>
{
    private SolverTask<T> task;

    public SolverTask<T> Task => this.task;

    public static SolverTaskMethodBuilder<T> Create() => new();

    public void Start<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

    public void SetStateMachine(IAsyncStateMachine _) => Debug.Fail("Unused");

    public void SetException(Exception exception) => this.Task.Awaiter.SetResult(default, exception);
    public void SetResult(T result) => this.Task.Awaiter.SetResult(result, null);

    public void AwaitOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (awaiter is not SolverTaskAwaiter<object> syncAwaiter)
        {
            throw new NotSupportedException("Only supporting SolverTasks.");
        }
        this.task.Awaiter.Solver = syncAwaiter.Solver;
        awaiter.OnCompleted(stateMachine.MoveNext);
    }

    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
        ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        if (awaiter is not SolverTaskAwaiter<object> syncAwaiter)
        {
            throw new NotSupportedException("Only supporting SolverTasks.");
        }
        this.task.Awaiter.Solver = syncAwaiter.Solver;
        awaiter.OnCompleted(stateMachine.MoveNext);
    }
}
