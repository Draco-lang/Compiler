using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Draco.Coverage;

namespace Draco.Fuzzing;

/// <summary>
/// Executes the fuzzed target.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface ITargetExecutor<TInput>
{
    /// <summary>
    /// Called once at the start of the fuzzing process.
    /// </summary>
    public void GlobalInitializer();

    /// <summary>
    /// Initializes the target for execution.
    /// Called before each execution.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <returns>The target information.</returns>
    public TargetInfo Initialize(TInput input);

    /// <summary>
    /// Executes the target.
    /// </summary>
    /// <param name="targetInfo">The target information.</param>
    /// <param name="input">The input data.</param>
    public void Execute(TargetInfo targetInfo, TInput input);
}

/// <summary>
/// Factory for creating built-in target executors.
/// </summary>
public sealed class TargetExecutor
{
    /// <summary>
    /// Creates an in-process target executor.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="assembly">The instrumented assembly.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The target executor.</returns>
    public static ITargetExecutor<TInput> InProcess<TInput>(InstrumentedAssembly assembly, Action<TInput> action) =>
        new InProcessExecutor<TInput>(assembly, action);

    /// <summary>
    /// Creates an out-of-process target executor.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="assembly">The instrumented assembly.</param>
    /// <param name="createStartInfo">The function to create the process start info from the input.</param>
    /// <returns>The target executor.</returns>
    public static ITargetExecutor<TInput> OutOfProcess<TInput>(
        InstrumentedAssembly assembly,
        Func<TInput, ProcessStartInfo> createStartInfo) =>
        new OutOfProcessExecutor<TInput>(assembly, createStartInfo);

    private sealed class InProcessExecutor<TInput>(InstrumentedAssembly assembly, Action<TInput> action) : ITargetExecutor<TInput>
    {
        public void GlobalInitializer()
        {
            // Run all type constructors
            foreach (var type in assembly.WeavedAssembly.GetTypes())
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
        }

        public TargetInfo Initialize(TInput input) => TargetInfo.InProcess(assembly);

        public void Execute(TargetInfo targetInfo, TInput input) => action(input);
    }

    private sealed class OutOfProcessExecutor<TInput>(
        InstrumentedAssembly assembly,
        Func<TInput, ProcessStartInfo> createStartInfo) : ITargetExecutor<TInput>
    {
        public void GlobalInitializer() { }

        public TargetInfo Initialize(TInput input)
        {
            var startInfo = createStartInfo(input);
            // Share memory with the process
            var memoryId = Guid.NewGuid().ToString();
            var sharedMemoryKey = InstrumentedAssembly.SharedMemoryKey(memoryId);
            var sharedMemory = SharedMemory.CreateNew<int>(sharedMemoryKey, assembly.SequencePoints.Length);
            startInfo.EnvironmentVariables.Add(InstrumentedAssembly.SharedMemoryEnvironmentVariable, memoryId);
            // Actually create the process
            var process = new Process
            {
                StartInfo = startInfo,
            };
            return TargetInfo.OutOfProcess(assembly, process, sharedMemory);
        }

        public void Execute(TargetInfo targetInfo, TInput input)
        {
            if (targetInfo.Process is null)
            {
                throw new ArgumentException("must have a process to execute out-of-process", nameof(targetInfo));
            }
            if (targetInfo.SharedMemory is null)
            {
                throw new ArgumentException("must have shared memory to execute out-of-process", nameof(targetInfo));
            }
            targetInfo.Process.Start();
        }
    }
}
