using System;

namespace Draco.Fuzzing;

/// <summary>
/// Settings for the fuzzer.
/// </summary>
public sealed record class FuzzerSettings
{
    /// <summary>
    /// The default settings for an in-process fuzzer.
    /// </summary>
    public static FuzzerSettings DefaultInProcess { get; } = new() { MaxDegreeOfParallelism = -1 };

    /// <summary>
    /// The default settings for an out-of-process fuzzer.
    /// </summary>
    public static FuzzerSettings DefaultOutOfProcess { get; } = new();

    /// <summary>
    /// The seed to use for the random number generator.
    /// </summary>
    public int Seed { get; init; } = Random.Shared.Next();

    /// <summary>
    /// The maximum number of parallelism. -1 means unlimited.
    /// </summary>
    public int MaxDegreeOfParallelism
    {
        get => this.maxDegreeOfParallelism;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, -1, nameof(value));
            ArgumentOutOfRangeException.ThrowIfZero(value, nameof(value));
            this.maxDegreeOfParallelism = value;
        }
    }
    private int maxDegreeOfParallelism = -1;

    /// <summary>
    /// The size of the input queue needs to be before starting to compress the entries.
    /// -1 means no compression.
    /// </summary>
    public int CompressAfterQueueSize
    {
        get => this.compressAfterQueueSize;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, -1, nameof(value));
            this.compressAfterQueueSize = value;
        }
    }
    private int compressAfterQueueSize = -1;
}
