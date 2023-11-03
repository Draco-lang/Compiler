using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Solver.Tasks;
#pragma warning disable IDE0250 // Make struct 'readonly'
public class SolverTaskCompletionSource<T>
{
    internal SolverTaskCompletionSource(ConstraintSolver solver)
    {
        this.Awaiter.Solver = solver;
    }

    internal ConstraintSolver Solver => this.Awaiter.Solver;
    internal SolverTaskAwaiter<T> Awaiter;
    public bool IsCompleted => this.Awaiter.IsCompleted;
    public T Result => this.Awaiter.GetResult();
    public SolverTaskAwaiter<T> GetAwaiter() => this.Awaiter;
    public void SetResult(T result) => this.Awaiter.SetResult(result, null);
    public void SetException(Exception exception) => this.Awaiter.SetResult(default, exception);
}
