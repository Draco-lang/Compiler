using System;
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

/// <summary>
/// Factory for common target execution logic.
/// </summary>
public static class TargetExecutor
{
    /// <summary>
    /// Creates a target executor from the given action.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <returns>The target executor.</returns>
    public static ITargetExecutor<TInput> Create<TInput>(Action<TInput> action) =>
        new DelegateTargetExecutor<TInput>(action);

    private sealed class DelegateTargetExecutor<TInput>(Action<TInput> action) : ITargetExecutor<TInput>
    {
        public void Execute(TInput input, InstrumentedAssembly assembly) => action(input);
    }
}
