using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Solver.Tasks;
[AsyncMethodBuilder(typeof(SolverTaskMethodBuilder<>))]
internal struct SolverTask<T>
{
    internal SolverTaskAwaiter<T> Awaiter;
    internal readonly ConstraintSolver Solver => this.Awaiter.Solver;
    public readonly bool IsCompleted => this.Awaiter.IsCompleted;
    public readonly T Result => this.Awaiter.GetResult();
    public readonly SolverTaskAwaiter<T> GetAwaiter() => this.Awaiter;
}
