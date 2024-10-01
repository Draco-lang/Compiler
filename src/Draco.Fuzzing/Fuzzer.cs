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
/// A fuzzer loop, which generates test cases and runs them against a target.
/// The method is inspired by the famous AFL fuzzer, which is basically doing 3 steps in a loop:
///  1. Load a test case from the queue
///  2. Minimize the test case
///  3. Mutate the test case
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
/// <typeparam name="TCompressedInput">The type of the compressed input data.</typeparam>
/// <typeparam name="TCoverage">The type of the compressed coverage data.</typeparam>
public sealed class Fuzzer<TInput, TCompressedInput, TCoverage>
    where TCoverage : notnull
{
    // Minimal result info of an execution
    private readonly record struct ExecutionResult(TCoverage Coverage, FaultResult FaultResult);

    // Initial inputs have no coverage data, so the entry needs to handle the case where coverage is not yet present
    // and we fill it out later
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
            var input = this.GetInput(fuzzer);
            return this.executionResult ??= fuzzer.Execute(input, existingId: this.inputId).Result;
        }
    }

    /// <summary>
    /// The seed to use for the random number generator.
    /// </summary>
    public int Seed { get; }

    /// <summary>
    /// The maximum number of parallelism. -1 means unlimited.
    /// </summary>
    public int MaxDegreeOfParallelism { get; }

    /// <summary>
    /// A shared random number generator.
    /// </summary>
    public Random Random { get; }

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

    /// <summary>
    /// The tracer to use.
    /// </summary>
    public required ITracer<TInput> Tracer { get; init; }

    private readonly BlockingCollection<QueueEntry> inputQueue = new(new ConcurrentQueue<QueueEntry>());
    private readonly ConcurrentHashSet<TCoverage> seenCoverages = [];
    private readonly object tracerSync = new();
    private int inputIdCounter = 0;

    public Fuzzer(int? seed = null, int maxDegreeOfParallelism = -1)
    {
        ArgumentOutOfRangeException.ThrowIfZero(maxDegreeOfParallelism, nameof(maxDegreeOfParallelism));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, -1, nameof(maxDegreeOfParallelism));

        this.Seed = seed ?? Random.Shared.Next();
        this.MaxDegreeOfParallelism = maxDegreeOfParallelism;
        this.Random = new Random(this.Seed);
    }

    private int GetNextInputId() => Interlocked.Increment(ref this.inputIdCounter);

    /// <summary>
    /// Enqueues the given input into the fuzzer.
    /// </summary>
    /// <param name="input">The input to enqueue.</param>
    public void Enqueue(TInput input)
    {
        var entry = this.MakeQueueEntry(input);
        this.inputQueue.Add(entry);
        lock (this.tracerSync) this.Tracer.InputsEnqueued([entry.GetInputWithId(this)]);
    }

    /// <summary>
    /// Enqueues a range of inputs into the fuzzer.
    /// </summary>
    /// <param name="inputs">The inputs to enqueue.</param>
    public void EnqueueRange(IEnumerable<TInput> inputs)
    {
        var entries = inputs
            .Select(i => this.MakeQueueEntry(i))
            .ToList();
        foreach (var entry in entries) this.inputQueue.Add(entry);

        var inputsWithId = entries
            .Select(e => e.GetInputWithId(this))
            .ToList();
        lock (this.tracerSync) this.Tracer.InputsEnqueued(inputsWithId);
    }

    /// <summary>
    /// Runs the fuzzing loop.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to stop the loop.</param>
    public void Run(CancellationToken cancellationToken = default)
    {
        // First off, make sure the executor is set up
        // For example, in-process execution will need to run all type constructors here
        // The reason is to not poison the coverage data with all the setup code
        this.TargetExecutor.GlobalInitializer();
        lock (this.tracerSync) this.Tracer.FuzzerStarted();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = this.MaxDegreeOfParallelism,
        };
        var limitedPartitioner = Partitioner.Create(
            this.inputQueue.GetConsumingEnumerable(cancellationToken),
            EnumerablePartitionerOptions.NoBuffering);
        Parallel.ForEach(limitedPartitioner, parallelOptions, entry =>
        {
            lock (this.tracerSync) this.Tracer.InputDequeued(entry.GetInputWithId(this));

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
        lock (this.tracerSync) this.Tracer.FuzzerFinished();
    }

    private QueueEntry Minimize(QueueEntry entry)
    {
        var referenceResult = entry.GetExecutionResult(this);
        // While we find a minimization step, we continue to minimize
        while (true)
        {
            var entryInput = entry.GetInputWithId(this);
            foreach (var minimizedInput in this.InputMinimizer.Minimize(this.Random, entryInput.Input))
            {
                var (minimizedResult, _, id) = this.Execute(minimizedInput);
                if (AreEqualExecutions(referenceResult, minimizedResult))
                {
                    // We found an equivalent execution, replace entry
                    lock (this.tracerSync) this.Tracer.MinimizationFound(entryInput, new(id, minimizedInput));
                    entry = this.MakeQueueEntry(minimizedInput, minimizedResult);
                    goto found;
                }
            }
            // No minimization found
            break;
        found:;
        }
        return entry;
    }

    private void Mutate(QueueEntry entry)
    {
        var entryInput = entry.GetInputWithId(this);
        foreach (var mutatedInput in this.InputMutator.Mutate(this.Random, entryInput.Input))
        {
            var (_, isInteresting, id) = this.Execute(mutatedInput);
            if (isInteresting)
            {
                lock (this.tracerSync) this.Tracer.MutationFound(entryInput, new(id, mutatedInput));
            }
        }
    }

    private (ExecutionResult Result, bool IsInteresting, int Id) Execute(TInput input, int existingId = -1)
    {
        var targetInfo = this.TargetExecutor.Initialize(input);
        this.CoverageReader.Clear(targetInfo);
        var inputId = existingId == -1 ? this.GetNextInputId() : existingId;
        var inputWithId = new InputWithId<TInput>(inputId, input);
        lock (this.tracerSync) this.Tracer.InputFuzzStarted(inputWithId, targetInfo);
        var faultResult = this.FaultDetector.Detect(this.TargetExecutor, targetInfo);
        if (faultResult.IsFaulted)
        {
            lock (this.tracerSync) this.Tracer.InputFaulted(inputWithId, faultResult);
        }
        var coverage = this.CoverageReader.Read(targetInfo);
        lock (this.tracerSync) this.Tracer.InputFuzzEnded(inputWithId, targetInfo, coverage);
        var compressedCoverage = this.CoverageCompressor.Compress(coverage);
        var isInteresting = this.IsInteresting(compressedCoverage);
        var executionResult = new ExecutionResult(compressedCoverage, faultResult);
        // If existing id was not -1, the input was already a queue entry, don't requeue
        if (existingId == -1 && isInteresting)
        {
            var entry = this.MakeQueueEntry(input, executionResult, id: inputId);
            this.inputQueue.Add(entry);
            lock (this.tracerSync) this.Tracer.InputsEnqueued([entry.GetInputWithId(this)]);
        }
        return (executionResult, isInteresting, inputId);
    }

    private QueueEntry MakeQueueEntry(TInput input, ExecutionResult? executionResult = null, int id = -1)
    {
        if (id == -1) id = this.GetNextInputId();
        if (this.inputQueue.Count > 5000)
        {
            // Compress
            var compressedInput = this.InputCompressor.Compress(input);
            return new QueueEntry(id, compressedInput, executionResult);
        }
        else
        {
            // Don't compress
            return new QueueEntry(id, input, executionResult);
        }
    }

    // We deem an input interesting, if it has not been seen before in terms of coverage
    private bool IsInteresting(TCoverage coverage) => this.seenCoverages.Add(coverage);

    // We deem them equal if they cover the same code and have the same fault result
    private static bool AreEqualExecutions(ExecutionResult a, ExecutionResult b) =>
           EqualityComparer<TCoverage>.Default.Equals(a.Coverage, b.Coverage)
        && FaultEqualityComparer.Instance.Equals(a.FaultResult, b.FaultResult);
}
