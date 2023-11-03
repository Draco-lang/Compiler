using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Binding.Tasks;

internal interface IBindingTaskAwaiter
{
    public ConstraintSolver Solver { get; set; }
}
