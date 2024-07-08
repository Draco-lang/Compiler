using System.Collections.Immutable;

namespace Draco.Compiler.Fuzzer.Generators;

/// <summary>
/// An <see cref="IGenerator{T}"/> that generates a sequence based on another generator.
/// </summary>
/// <typeparam name="TElement">The element type this sequence input generator generates.</typeparam>
internal sealed class SequenceGenerator<TElement>(
    IGenerator<TElement> elementGenerator,
    SequenceGenerationSettings? settings = null) : IGenerator<ImmutableArray<TElement>>
{
    public SequenceGenerationSettings Settings { get; } = settings ?? SequenceGenerationSettings.Default;

    private readonly ImmutableArray<TElement>.Builder sequence = ImmutableArray.CreateBuilder<TElement>();
    private readonly Random random = new();

    public ImmutableArray<TElement> NextEpoch()
    {
        this.sequence.Clear();
        var length = this.random.Next(this.Settings.MinLength, this.Settings.MaxLength);
        for (var i = 0; i < length; ++i) this.sequence.Add(elementGenerator.NextEpoch());
        return this.sequence.ToImmutableArray();
    }

    public ImmutableArray<TElement> NextMutation()
    {
        // Determine splice parameters
        var spliceStart = this.random.Next(this.sequence.Count);
        var removeLength = this.random.Next(this.Settings.MinRemove, this.Settings.MaxRemove);
        var insertLength = this.random.Next(this.Settings.MinInsert, this.Settings.MaxInsert);

        // Do the splice
        this.sequence.RemoveRange(spliceStart, Math.Min(removeLength, this.sequence.Count - spliceStart));
        for (var i = 0; i < insertLength; ++i) this.sequence.Insert(spliceStart + i, elementGenerator.NextEpoch());

        // Done
        return this.sequence.ToImmutableArray();
    }

    public string ToString(ImmutableArray<TElement> value) =>
        $"[{string.Join(", ", value.Select(elementGenerator.ToString))}]";
}
