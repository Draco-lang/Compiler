using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Binding.Tasks;

[AsyncMethodBuilder(typeof(BindingTaskMethodBuilder<>))]
internal struct BindingTask<T>
{
    internal BindingTaskAwaiter<T> Awaiter;
    internal readonly ConstraintSolver Solver => this.Awaiter.Solver;
    public readonly bool IsCompleted => this.Awaiter.IsCompleted;
    public readonly T Result => this.Awaiter.GetResult();
    public readonly BindingTaskAwaiter<T> GetAwaiter() => this.Awaiter;
}
