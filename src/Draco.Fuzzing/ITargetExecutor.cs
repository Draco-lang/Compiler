using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Draco.Coverage;

namespace Draco.Fuzzing;

/// <summary>
/// A type that executes the target.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface ITargetExecutor<TInput>
{
    /// <summary>
    /// The global initializer for the target.
    /// This will run only once before an execution.
    /// </summary>
    public void Initialize();

    /// <summary>
    /// Executes the target with the given input.
    /// </summary>
    /// <param name="input">The input to execute the target with.</param>
    public void Execute(TInput input);
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
    /// <param name="init">The action to initialize the target.</param>
    /// <param name="execute">The action to execute.</param>
    /// <returns>The target executor.</returns>
    public static ITargetExecutor<TInput> Create<TInput>(Action init, Action<TInput> execute) =>
        new DelegateTargetExecutor<TInput>(init, execute);

    /// <summary>
    /// Creates a target executor that executes the given action after initializing the instrumented assembly.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="assembly">The instrumented assembly to initialize.</param>
    /// <param name="execute">The action to execute.</param>
    /// <returns>The target executor.</returns>
    public static ITargetExecutor<TInput> Assembly<TInput>(InstrumentedAssembly assembly, Action<TInput> execute)
    {
        var typeCtorsRan = false;
        return Create(
            init: () =>
            {
                if (typeCtorsRan) return;
                foreach (var type in assembly.WeavedAssembly.GetTypes())
                {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
                typeCtorsRan = true;
            },
            execute: execute);
    }

    /// <summary>
    /// Creates a target executor that starts an external process. The process is not waited for to finish.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="assembly">The instrumented assembly to read metadata from.</param>
    /// <param name="func">The function to create the process start info from the input.</param>
    /// <param name="processReference">The reference to the process being executed.</param>
    /// <returns>The target executor.</returns>
    public static ITargetExecutor<TInput> Process<TInput>(
        InstrumentedAssembly assembly,
        Func<TInput, ProcessStartInfo> func,
        out ProcessReference processReference)
    {
        var processRef = new ProcessReference();
        processReference = processRef;
        return Create<TInput>(() => { }, input =>
        {
            var startInfo = func(input);
            // Share memory with the process
            var memoryId = Guid.NewGuid().ToString();
            var sharedMemoryKey = InstrumentedAssembly.SharedMemoryKey(memoryId);
            var sharedMemory = SharedMemory.CreateNew<int>(sharedMemoryKey, assembly.SequencePoints.Length);
            startInfo.EnvironmentVariables.Add(InstrumentedAssembly.SharedMemoryEnvironmentVariable, memoryId);
            // Create process, write to process reference
            var process = new Process { StartInfo = startInfo };
            processRef.Process = process;
            processRef.SharedMemory = sharedMemory;
            process.Start();
        });
    }

    private sealed class DelegateTargetExecutor<TInput>(
        Action init,
        Action<TInput> execute) : ITargetExecutor<TInput>
    {
        public void Initialize() => init();
        public void Execute(TInput input) => execute(input);
    }
}
