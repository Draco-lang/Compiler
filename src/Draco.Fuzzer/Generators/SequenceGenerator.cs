using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// An <see cref="IInputGenerator{T}"/> that generates a sequence based on another generator.
/// </summary>
/// <typeparam name="TElement">The element type this sequence input generator generates.</typeparam>
internal sealed class SequenceGenerator<TElement> : IInputGenerator<ImmutableArray<TElement>>
{
    /// <summary>
    /// The minimum default length of the sequence.
    /// </summary>
    public int MinLength { get; set; } = 0;

    /// <summary>
    /// The maximum default length of the sequence.
    /// </summary>
    public int MaxLength { get; set; } = 100;

    /// <summary>
    /// The minimum number of elements to remove in a mutation.
    /// </summary>
    public int MinRemove { get; set; } = 0;

    /// <summary>
    /// The maximum number of elements to remove in a mutation.
    /// </summary>
    public int MaxRemove { get; set; } = 10;

    /// <summary>
    /// The minimum number of elements to insert in a mutation.
    /// </summary>
    public int MinInsert { get; set; } = 0;

    /// <summary>
    /// The maximum number of elements to insert in a mutation.
    /// </summary>
    public int MaxInsert { get; set; } = 10;

    private readonly IInputGenerator<TElement> elementGenerator;
    private readonly ImmutableArray<TElement>.Builder sequence = ImmutableArray.CreateBuilder<TElement>();
    private readonly Random random = new();

    public SequenceGenerator(IInputGenerator<TElement> elementGenerator)
    {
        this.elementGenerator = elementGenerator;
    }

    public ImmutableArray<TElement> NextExpoch()
    {
        this.sequence.Clear();
        var length = this.random.Next(this.MinLength, this.MaxLength);
        for (var i = 0; i < length; ++i) this.sequence.Add(this.elementGenerator.NextExpoch());
        return this.sequence.ToImmutableArray();
    }

    public ImmutableArray<TElement> NextMutation()
    {
        // Determine splice parameters
        var spliceStart = this.random.Next(this.sequence.Count);
        var removeLength = this.random.Next(this.MinRemove, this.MaxRemove);
        var insertLength = this.random.Next(this.MinInsert, this.MaxInsert);

        // Do the splice
        this.sequence.RemoveRange(spliceStart, Math.Min(removeLength, this.sequence.Count - spliceStart));
        for (var i = 0; i < insertLength; ++i) this.sequence.Insert(spliceStart + i, this.elementGenerator.NextExpoch());

        // Done
        return this.sequence.ToImmutableArray();
    }

    public string ToString(ImmutableArray<TElement> value) =>
        $"[{string.Join(", ", value.Select(this.elementGenerator.ToString))}]";
}
