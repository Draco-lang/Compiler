using System;
using System.Diagnostics;
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

    /// <summary>
    /// Creates a target executor that starts an external process. The process is not waited for to finish.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="func">The function to create the process start info from the input.</param>
    /// <param name="processReference">The reference to the process being executed.</param>
    /// <returns>The target executor.</returns>
    public static ITargetExecutor<TInput> Process<TInput>(
        Func<TInput, ProcessStartInfo> func,
        out ProcessReference processReference)
    {
        var processRef = new ProcessReference();
        processReference = processRef;
        return Create<TInput>(input =>
        {
            var startInfo = func(input);
            var process = new Process { StartInfo = startInfo };
            processRef.Process = process;
            process.Start();
        });
    }

    private sealed class DelegateTargetExecutor<TInput>(Action<TInput> action) : ITargetExecutor<TInput>
    {
        public void Execute(TInput input, InstrumentedAssembly assembly) => action(input);
    }
}
