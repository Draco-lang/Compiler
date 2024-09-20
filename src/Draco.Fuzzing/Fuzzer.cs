using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Draco.Coverage;

namespace Draco.Fuzzing;

/// <summary>
/// A fuzzer loop, which generates test cases and runs them against a target.
/// The method is inspired by the famous AFL fuzzer, which is basically doing 3 steps in a loop:
///  1. Load a test case from the queue
///  2. Minimize the test case
///  3. Mutate the test case
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
/// <typeparam name="TCoverage">The type of the compressed coverage data.</typeparam>
public sealed class Fuzzer<TInput, TCoverage>
{
    private readonly record struct ExecutionResult(
        FaultResult FaultResult,
        CoverageResult UncompressedCoverage,
        TCoverage CompressedCoverage);

    /// <summary>
    /// The target to fuzz.
    /// </summary>
    public required InstrumentedAssembly InstrumentedAssembly { get; init; }

    /// <summary>
    /// The input minimizer to use.
    /// </summary>
    public required IInputMinimizer<TInput> InputMinimizer { get; init; }

    /// <summary>
    /// The input mutator to use.
    /// </summary>
    public required IInputMutator<TInput> InputMutator { get; init; }

    /// <summary>
    /// The coverage compressor to use.
    /// </summary>
    public required ICoverageCompressor<TCoverage> CoverageCompressor { get; init; }

    /// <summary>
    /// The target executor to use.
    /// </summary>
    public required ITargetExecutor<TInput> TargetExecutor { get; init; }

    /// <summary>
    /// The fault detector to use.
    /// </summary>
    public required IFaultDetector FaultDetector { get; init; }

    /// <summary>
    /// The tracer to use.
    /// </summary>
    public required ITracer<TInput> Tracer { get; init; }

    private readonly Queue<TInput> inputQueue = new();
    private readonly HashSet<TCoverage> discoveredCoverages = new();
    private readonly Stopwatch stopwatch = new();

    /// <summary>
    /// Enqueues the given input.
    /// </summary>
    /// <param name="input">The input to enqueue.</param>
    public void Enqueue(TInput input) => this.inputQueue.Enqueue(input);

    /// <summary>
    /// Enqueues the given inputs.
    /// </summary>
    /// <param name="inputs">The inputs to enqueue.</param>
    public void EnqueueRange(IEnumerable<TInput> inputs)
    {
        foreach (var input in inputs) this.inputQueue.Enqueue(input);
    }

    /// <summary>
    /// Fuzzes the given target.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the fuzzing process.</param>
    public void Fuzz(CancellationToken cancellationToken)
    {
        this.stopwatch.Start();
        while (!cancellationToken.IsCancellationRequested && this.inputQueue.TryDequeue(out var input))
        {
            var start = this.stopwatch.Elapsed;
            // Minimize the input
            var (minimalInput, executionResult) = this.Minimize(input);
            var endOfMinimization = this.stopwatch.Elapsed;
            this.Tracer.EndOfMinimization(input, minimalInput, executionResult.UncompressedCoverage, endOfMinimization - start);
            // Test for fault
            if (executionResult.FaultResult.IsFaulted)
            {
                this.Tracer.InputFaulted(minimalInput, executionResult.FaultResult);
                // NOTE: Should we not skip mutating this input?
                continue;
            }
            // Mutate the input
            var mutationsFound = 0;
            foreach (var mutated in this.Mutate(minimalInput, executionResult.CompressedCoverage))
            {
                this.Enqueue(mutated);
                ++mutationsFound;
            }
            var endOfMutations = this.stopwatch.Elapsed;
            this.Tracer.EndOfMutations(minimalInput, mutationsFound, endOfMutations - endOfMinimization);
        }
    }

    private (TInput Input, ExecutionResult Result) Minimize(TInput input)
    {
        // Compute the reference result
        var referenceResult = this.ExecuteTarget(input);
        // While we find a minimization step, we continue to minimize
        while (true)
        {
            foreach (var minimized in this.InputMinimizer.Minimize(input))
            {
                var minimizedResult = this.ExecuteTarget(minimized);
                if (AreEqualExecutions(referenceResult, minimizedResult))
                {
                    input = minimized;
                    goto found;
                }
            }
            // We did not find a minimized input in this iteration
            break;
        found:;
        }
        // Return whatever we could minimize
        return (input, referenceResult);
    }

    private IEnumerable<TInput> Mutate(TInput input, TCoverage referenceCoverage)
    {
        foreach (var mutatedInput in this.InputMutator.Mutate(input))
        {
            var executionResult = this.ExecuteTarget(mutatedInput);
            if (this.IsConsideredInteresting(executionResult.CompressedCoverage))
            {
                yield return mutatedInput;
            }
        }
    }

    private ExecutionResult ExecuteTarget(TInput input)
    {
        this.InstrumentedAssembly.ClearCoverageData();
        var faultResult = this.FaultDetector.Execute(() => this.TargetExecutor.Execute(input, this.InstrumentedAssembly));
        var coverage = this.InstrumentedAssembly.GetCoverageResult();
        var compressedCoverage = this.CoverageCompressor.Compress(coverage);
        return new(faultResult, coverage, compressedCoverage);
    }

    // NOTE: For now we just check if the compressed coverage is new in a set
    private bool IsConsideredInteresting(TCoverage coverage) => this.discoveredCoverages.Add(coverage);

    private static bool AreEqualExecutions(ExecutionResult a, ExecutionResult b) =>
           AreEqualFaults(a.FaultResult, b.FaultResult)
        && Equals(a.CompressedCoverage, b.CompressedCoverage);

    private static bool AreEqualFaults(FaultResult a, FaultResult b) =>
           a.ThrownException?.GetType() == b.ThrownException?.GetType()
        && (a.TimeoutReached is null) == (b.TimeoutReached is null)
        // For safety we check this too. Redundant but if we extend the type, we might forget to update this method.
        && a.IsFaulted == b.IsFaulted;
}