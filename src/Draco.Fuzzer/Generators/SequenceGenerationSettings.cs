using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Settings for sequence generation.
/// </summary>
internal sealed class SequenceGenerationSettings
{
    /// <summary>
    /// The default settings.
    /// </summary>
    public static SequenceGenerationSettings Default { get; } = new();

    /// <summary>
    /// The minimum default length of the sequence.
    /// </summary>
    public int MinLength { get; init; } = 0;

    /// <summary>
    /// The maximum default length of the sequence.
    /// </summary>
    public int MaxLength { get; init; } = 100;

    /// <summary>
    /// The minimum number of elements to remove in a mutation.
    /// </summary>
    public int MinRemove { get; init; } = 0;

    /// <summary>
    /// The maximum number of elements to remove in a mutation.
    /// </summary>
    public int MaxRemove { get; init; } = 10;

    /// <summary>
    /// The minimum number of elements to insert in a mutation.
    /// </summary>
    public int MinInsert { get; init; } = 0;

    /// <summary>
    /// The maximum number of elements to insert in a mutation.
    /// </summary>
    public int MaxInsert { get; init; } = 10;
}
