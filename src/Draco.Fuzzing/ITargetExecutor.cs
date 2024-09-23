using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
        // We need to load the assembly to have an idea about the size of shared memory we need to allocate
        // This dictionary maps the full path of the assembly to the number of sequence points in it
        // There is very likely that this dictionary will only have a single entry, but since we have no
        // idea what user-code produces for start info, we'll just do this
        var loadedAssemblies = new Dictionary<string, int>();

        int GetNumberOfSequencePoints(string pathToProcess)
        {
            if (!loadedAssemblies.TryGetValue(pathToProcess, out var sequencePointCount))
            {
                var assembly = Assembly.LoadFrom(pathToProcess);
                var instrumentedAssembly = InstrumentedAssembly.FromWeavedAssembly(assembly);
                sequencePointCount = instrumentedAssembly.SequencePoints.Length;
            }
            return sequencePointCount;
        }

        var processRef = new ProcessReference();
        processReference = processRef;
        return Create<TInput>(input =>
        {
            var startInfo = func(input);
            // Share memory with the process
            var memoryId = Guid.NewGuid().ToString();
            var sharedMemoryKey = InstrumentedAssembly.SharedMemoryKey(memoryId);
            var sharedMemory = SharedMemory.CreateNew<int>(sharedMemoryKey, GetNumberOfSequencePoints(startInfo.FileName));
            startInfo.EnvironmentVariables.Add(InstrumentedAssembly.SharedMemoryEnvironmentVariable, memoryId);
            // Create process, write to process reference
            var process = new Process { StartInfo = startInfo };
            processRef.Process = process;
            processRef.SharedMemory = sharedMemory;
            process.Start();
        });
    }

    private sealed class DelegateTargetExecutor<TInput>(Action<TInput> action) : ITargetExecutor<TInput>
    {
        public void Execute(TInput input, InstrumentedAssembly assembly) => action(input);
    }
}
