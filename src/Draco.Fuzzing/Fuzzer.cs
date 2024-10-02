using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Fuzzing.Components;
using Draco.Fuzzing.Tracing;
using Draco.Fuzzing.Utilities;

namespace Draco.Fuzzing;

/// <summary>
/// A nongeneric fuzzer interface.
/// </summary>
public interface IFuzzer
{
    /// <summary>
    /// Enqueues the given input into the fuzzer.
    /// </summary>
    /// <param name="input">The input to enqueue.</param>
    public void Enqueue(object? input);

    /// <summary>
    /// Enqueues a range of inputs into the fuzzer.
    /// </summary>
    /// <param name="inputs">The inputs to enqueue.</param>
    public void EnqueueRange(IEnumerable<object?> inputs);

    /// <summary>
    /// Runs the fuzzing loop.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to stop the loop.</param>
    public void Run(CancellationToken cancellationToken = default);
}

/// <summary>
/// A generic fuzzer interface.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
public interface IFuzzer<TInput> : IFuzzer
{
    /// <summary>
    /// The tracer to use.
    /// </summary>
    public ITracer<TInput> Tracer { get; }

    /// <summary>
    /// Enqueues the given input into the fuzzer.
    /// </summary>
    /// <param name="input">The input to enqueue.</param>
    public void Enqueue(TInput input);

    /// <summary>
    /// Enqueues a range of inputs into the fuzzer.
    /// </summary>
    /// <param name="inputs">The inputs to enqueue.</param>
    public void EnqueueRange(IEnumerable<TInput> inputs);
}

/// <summary>
/// A fuzzer loop, which generates test cases and runs them against a target.
/// The method is inspired by the famous AFL fuzzer, which is basically doing 3 steps in a loop:
///  1. Load a test case from the queue
///  2. Minimize the test case
///  3. Mutate the test case
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
/// <typeparam name="TCompressedInput">The type of the compressed input data.</typeparam>
/// <typeparam name="TCoverage">The type of the compressed coverage data.</typeparam>
public sealed class Fuzzer<TInput, TCompressedInput, TCoverage>(FuzzerSettings settings) : IFuzzer<TInput>
    where TCoverage : notnull
{
    /// <summary>
    /// Minimal information about an ececution.
    /// </summary>
    /// <param name="Coverage">The compressed coverage data.</param>
    /// <param name="FaultResult">The fault result of the execution.</param>
    private readonly record struct ExecutionResult(TCoverage Coverage, FaultResult FaultResult);

    /// <summary>
    /// A queue entry that might never have been ran before, or might have been compressed.
    /// </summary>
    private sealed class QueueEntry
    {
        private readonly int inputId;
        private readonly TCompressedInput? compressedInput;
        private bool isInputCompressed;
        private TInput? input;
        private ExecutionResult? executionResult;

        public QueueEntry(int inputId, TInput input, ExecutionResult? executionResult = null)
        {
            this.inputId = inputId;
            this.isInputCompressed = false;
            this.input = input;
            this.executionResult = executionResult;
        }

        public QueueEntry(int inputId, TCompressedInput compressedInput, ExecutionResult? executionResult = null)
        {
            this.inputId = inputId;
            this.isInputCompressed = true;
            this.compressedInput = compressedInput;
            this.executionResult = executionResult;
        }

        public InputWithId<TInput> GetInputWithId(Fuzzer<TInput, TCompressedInput, TCoverage> fuzzer) =>
            new(this.inputId, this.GetInput(fuzzer));

        public TInput GetInput(Fuzzer<TInput, TCompressedInput, TCoverage> fuzzer)
        {
            if (this.isInputCompressed)
            {
                this.input = fuzzer.InputCompressor.Decompress(this.compressedInput!);
                this.isInputCompressed = false;
            }
            return this.input!;
        }

        public ExecutionResult GetExecutionResult(Fuzzer<TInput, TCompressedInput, TCoverage> fuzzer)
        {
            var inputWithId = this.GetInputWithId(fuzzer);
            return this.executionResult ??= fuzzer.Execute(inputWithId);
        }
    }

    /// <summary>
    /// The settings for the fuzzer.
    /// </summary>
    public FuzzerSettings Settings { get; } = settings;

    /// <summary>
    /// A shared random number generator.
    /// </summary>
    public Random Random { get; } = new Random(settings.Seed);

    /// <summary>
    /// The input minimizer to use.
    /// </summary>
    public required IInputMinimizer<TInput> InputMinimizer { get; init; }

    /// <summary>
    /// The input mutator to use.
    /// </summary>
    public required IInputMutator<TInput> InputMutator { get; init; }

    /// <summary>
    /// The input compressor to use.
    /// </summary>
    public required IInputCompressor<TInput, TCompressedInput> InputCompressor { get; init; }

    /// <summary>
    /// The reader to read coverage data with.
    /// </summary>
    public required ICoverageReader CoverageReader { get; init; }

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

    public required ITracer<TInput> Tracer { get; init; }

    private readonly BlockingCollection<QueueEntry> inputQueue = new(new ConcurrentQueue<QueueEntry>());
    private readonly ConcurrentHashSet<TCoverage> seenCoverages = [];
    private int inputIdCounter = 0;

    void IFuzzer.Enqueue(object? input) => this.Enqueue((TInput)input!);
    void IFuzzer.EnqueueRange(IEnumerable<object?> inputs) => this.EnqueueRange(inputs.Cast<TInput>());

    public void Enqueue(TInput input)
    {
        var inputWithId = this.IdentifyInput(input);
        var entry = this.MakeQueueEntry(inputWithId);
        this.inputQueue.Add(entry);
        // Notify tracer
        this.Tracer.InputsEnqueued([inputWithId]);
    }

    public void EnqueueRange(IEnumerable<TInput> inputs)
    {
        // First we identify the inputs and make queue entries
        var entries = inputs
            .Select(i => this.MakeQueueEntry(this.IdentifyInput(i)))
            .ToList();
        foreach (var entry in entries) this.inputQueue.Add(entry);

        // Then we notify the tracer
        var inputsWithId = entries
            .Select(e => e.GetInputWithId(this))
            .ToList();
        this.Tracer.InputsEnqueued(inputsWithId);
    }

    public void Run(CancellationToken cancellationToken = default)
    {
        // First off, make sure the executor is set up
        // For example, in-process execution will need to run all type constructors here
        // The reason is to not poison the coverage data with all the setup code
        this.TargetExecutor.GlobalInitializer();
        this.Tracer.FuzzerStarted();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = this.Settings.MaxDegreeOfParallelism,
        };
        var limitedPartitioner = Partitioner.Create(
            this.inputQueue.GetConsumingEnumerable(cancellationToken),
            EnumerablePartitionerOptions.NoBuffering);
        Parallel.ForEach(limitedPartitioner, parallelOptions, entry =>
        {
            this.Tracer.InputDequeued(entry.GetInputWithId(this));

            // We want to minimize the input first
            entry = this.Minimize(entry);
            if (entry.GetExecutionResult(this).FaultResult.IsFaulted == true)
            {
                // NOTE: For now we don't mutate faulted results
                return;
            }

            // And we want to mutate the minimized input
            this.Mutate(entry);
        });
        this.Tracer.FuzzerFinished();
    }

    /// <summary>
    /// Attempts to minimize the input, using the registered <see cref="InputMinimizer"/>.
    /// </summary>
    /// <param name="entry">The entry to minimize.</param>
    /// <returns>The minimized entry.</returns>
    private QueueEntry Minimize(QueueEntry entry)
    {
        var referenceResult = entry.GetExecutionResult(this);
        // While we find a minimization step, we continue to minimize
        while (true)
        {
            var entryInput = entry.GetInputWithId(this);
            foreach (var minimizedInput in this.InputMinimizer.Minimize(this.Random, entryInput.Input))
            {
                var minimizedInputWithId = this.IdentifyInput(minimizedInput);
                var executionResult = this.Execute(minimizedInputWithId);
                if (AreEqualExecutions(referenceResult, executionResult))
                {
                    // We found an equivalent execution, replace entry
                    this.Tracer.MinimizationFound(entryInput, minimizedInputWithId);
                    entry = this.MakeQueueEntry(minimizedInputWithId, executionResult);
                    goto found;
                }
                else if (this.AddToQueueIfInteresting(minimizedInputWithId, executionResult))
                {
                    // New mutation found by minimization, notify tracer
                    this.Tracer.MutationFound(entryInput, minimizedInputWithId);
                }
            }
            // No minimization found
            break;
        found:;
        }
        return entry;
    }

    /// <summary>
    /// Attempts to mutate the input, using the registered <see cref="InputMutator"/>.
    /// All mutations that are deemed interesting are added to the queue.
    /// </summary>
    /// <param name="entry">The entry to mutate.</param>
    private void Mutate(QueueEntry entry)
    {
        var entryInputWithId = entry.GetInputWithId(this);
        foreach (var mutatedInput in this.InputMutator.Mutate(this.Random, entryInputWithId.Input))
        {
            var mutatedInputWithId = this.IdentifyInput(mutatedInput);
            var executionResult = this.Execute(mutatedInputWithId);
            // If the mutation is interesting, add it to the queue
            if (this.AddToQueueIfInteresting(mutatedInputWithId, executionResult))
            {
                // New mutation found, notify tracer
                this.Tracer.MutationFound(entryInputWithId, mutatedInputWithId);
            }
        }
    }

    /// <summary>
    /// Executes the fuzzed target with the given input.
    /// </summary>
    /// <param name="inputWithId">The input to feed to the target.</param>
    /// <returns>The result of the execution.</returns>
    private ExecutionResult Execute(InputWithId<TInput> inputWithId)
    {
        // Prepare an execution target
        var targetInfo = this.TargetExecutor.Initialize(inputWithId.Input);
        // Clear previous coverage info
        this.CoverageReader.Clear(targetInfo);
        // Notify tracer about the start
        this.Tracer.InputFuzzStarted(inputWithId, targetInfo);
        // Actually execute
        var faultResult = this.FaultDetector.Detect(this.TargetExecutor, targetInfo);
        // Read out coverage
        var coverage = this.CoverageReader.Read(targetInfo);
        // Notify tracer about the end
        this.Tracer.InputFuzzEnded(inputWithId, targetInfo, coverage);
        // Notify fault, if needed
        if (faultResult.IsFaulted) this.Tracer.InputFaulted(inputWithId, faultResult);
        // Compress coverage, keeping around the original is expensive
        var compressedCoverage = this.CoverageCompressor.Compress(coverage);
        // Done
        return new ExecutionResult(compressedCoverage, faultResult);
    }

    /// <summary>
    /// Adds the input to the queue, if the coverage is interesting.
    /// </summary>
    /// <param name="queueEntry">The queue entry to add.</param>
    /// <returns>True, if the entry was added, false otherwise.</returns>
    private bool AddToQueueIfInteresting(InputWithId<TInput> inputWithId, ExecutionResult executionResult)
    {
        // Check, if the coverage is interesting
        if (!this.IsInteresting(executionResult.Coverage)) return false;

        // If is, add to queue
        var queueEntry = this.MakeQueueEntry(inputWithId, executionResult);
        this.inputQueue.Add(queueEntry);
        this.Tracer.InputsEnqueued([inputWithId]);
        return true;
    }

    /// <summary>
    /// Creates a queue entry from the given input, compressing it if needed.
    /// </summary>
    /// <param name="inputWithId">The input to create the entry from.</param>
    /// <param name="executionResult">The execution result to add to the entry.</param>
    /// <returns>The created queue entry.</returns>
    private QueueEntry MakeQueueEntry(InputWithId<TInput> inputWithId, ExecutionResult? executionResult = null)
    {
        if (this.Settings.CompressAfterQueueSize != -1
         && this.inputQueue.Count > this.Settings.CompressAfterQueueSize)
        {
            // Need to compress the input
            var compressedInput = this.InputCompressor.Compress(inputWithId.Input);
            return new QueueEntry(inputWithId.Id, compressedInput, executionResult);
        }
        else
        {
            return new QueueEntry(inputWithId.Id, inputWithId.Input, executionResult);
        }
    }

    /// <summary>
    /// Adds a new identifier to the given input.
    /// </summary>
    /// <param name="input">The input to identify.</param>
    /// <returns>The input wrapped with an identifier.</returns>
    private InputWithId<TInput> IdentifyInput(TInput input) => new(this.GetNextInputId(), input);

    /// <summary>
    /// Checks, if the given coverage is determined to be interesting, which currently means that we haven't seen it before.
    /// Calling this method will add the coverage to the set of seen coverages.
    /// </summary>
    /// <param name="coverage">The coverage to check.</param>
    /// <returns>True, if the coverage is interesting, false otherwise.</returns>
    private bool IsInteresting(TCoverage coverage) => this.seenCoverages.Add(coverage);

    /// <summary>
    /// Allocates a new input identifier.
    /// </summary>
    /// <returns>The new input identifier.</returns>
    private int GetNextInputId() => Interlocked.Increment(ref this.inputIdCounter);

    /// <summary>
    /// Checks, if two executions are equal. We deem them equal if they cover the same code and have the same fault result.
    /// This is used for input equivalence checking, like for minimization.
    /// </summary>
    /// <param name="a">The first execution.</param>
    /// <param name="b">The second execution.</param>
    /// <returns>True, if the executions are equal, false otherwise.</returns>
    private static bool AreEqualExecutions(ExecutionResult a, ExecutionResult b) =>
           EqualityComparer<TCoverage>.Default.Equals(a.Coverage, b.Coverage)
        && FaultEqualityComparer.Instance.Equals(a.FaultResult, b.FaultResult);
}
