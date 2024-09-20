using Draco.Coverage;

namespace Draco.Fuzzing;

/// <summary>
/// A type that executes the target.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface ITargetExecutor<TInput>
{
    /// <summary>
    /// Executes the target with the given input.
    /// </summary>
    /// <param name="input">The input to execute the target with.</param>
    /// <param name="assembly">The instrumented assembly.</param>
    public void Execute(TInput input, InstrumentedAssembly assembly);
}
