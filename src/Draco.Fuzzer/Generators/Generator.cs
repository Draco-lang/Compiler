using System.Collections.Immutable;
using System.Globalization;

namespace Draco.Fuzzer.Generators;

/// <summary>
/// Utility functions for generation.
/// </summary>
internal static class Generator
{
    private sealed class DelegateGenerator<T> : IGenerator<T>
    {
        private readonly Func<T> nextEpoch;
        private readonly Func<T> nextMutation;
        private readonly Func<T, string> toString;

        public DelegateGenerator(
            Func<T> nextEpoch,
            Func<T> nextMutation,
            Func<T, string> toString)
        {
            this.nextEpoch = nextEpoch;
            this.nextMutation = nextMutation;
            this.toString = toString;
        }

        public T NextEpoch() => this.nextEpoch();
        public T NextMutation() => this.nextMutation();
        public string ToString(T value) => this.toString(value);
    }

    public static IGenerator<T> Delegate<T>(
        Func<T> nextEpoch,
        Func<T>? nextMutation = null,
        Func<T, string>? toString = null)
    {
        nextMutation ??= nextEpoch;
        toString ??= x => x?.ToString() ?? "null";
        return new DelegateGenerator<T>(nextEpoch, nextMutation, toString);
    }

    public static IGenerator<T> Pick<T>(params T[] elements) => Pick(elements.AsEnumerable());

    public static IGenerator<T> Pick<T>(IEnumerable<T> elements)
    {
        var rnd = new Random();
        var elementsArray = elements.ToArray();
        return Delegate(() => elementsArray[rnd.Next(0, elementsArray.Length)]);
    }

    public static IGenerator<int> Integer(int min, int max)
    {
        var rnd = new Random();
        return Delegate(() => rnd.Next(min, max));
    }

    public static IGenerator<double> Float(double min, double max)
    {
        var rnd = new Random();
        return Delegate(
            () => rnd.NextDouble() * (max - min) + min,
            toString: x => x.ToString(CultureInfo.InvariantCulture));
    }

    public static IGenerator<char> Character(string? charset = null) =>
        Pick((charset ?? Charsets.PrintableAscii).AsEnumerable());

    public static IGenerator<TEnum> EnumMember<TEnum>() where TEnum : Enum =>
        Integer(0, Enum.GetValues(typeof(TEnum)).Length).Map(x => (TEnum)(object)x);

    public static IGenerator<TNew> Map<TOld, TNew>(
        this IGenerator<TOld> generator,
        Func<TOld, TNew> map,
        Func<TNew, string>? toString = null) => Delegate(
            nextEpoch: () => map(generator.NextEpoch()),
            nextMutation: () => map(generator.NextMutation()),
            toString: toString);

    public static IGenerator<(T1 First, T2 Second)> Zip<T1, T2>(
        this IGenerator<T1> first,
        IGenerator<T2> second,
        Func<(T1 First, T2 Second), string>? toString = null) => Delegate(
            nextEpoch: () => (first.NextEpoch(), second.NextEpoch()),
            nextMutation: () => (first.NextMutation(), second.NextMutation()),
            toString: toString);

    public static IGenerator<ImmutableArray<T>> Sequence<T>(
        this IGenerator<T> element,
        SequenceGenerationSettings settings)
    {
        var generator = new SequenceGenerator<T>(element, settings);
        return generator;
    }

    public static IGenerator<ImmutableArray<T>> Sequence<T>(
        this IGenerator<T> element,
        int minLength = 0,
        int maxLength = 100,
        int minRemove = 0,
        int maxRemove = 10,
        int minInsert = 0,
        int maxInsert = 10) => element.Sequence(new SequenceGenerationSettings()
        {
            MinLength = minLength,
            MaxLength = maxLength,
            MinRemove = minRemove,
            MaxRemove = maxRemove,
            MinInsert = minInsert,
            MaxInsert = maxInsert,
        });

    public static IGenerator<ImmutableArray<T>> Append<T>(
        this IGenerator<ImmutableArray<T>> generator,
        T last) => Delegate(
            nextEpoch: () => generator.NextEpoch().Append(last).ToImmutableArray(),
            nextMutation: () => generator.NextMutation().Append(last).ToImmutableArray(),
            toString: generator.ToString);

    public static IGenerator<string> String(
        string? charset,
        SequenceGenerationSettings settings)
    {
        var charGenerator = Character(charset);
        var sequence = charGenerator.Sequence(settings ?? SequenceGenerationSettings.Default);
        return sequence.Map(seq => new string(seq.ToArray()));
    }

    public static IGenerator<string> String(
        string? charset = null,
        int minLength = 0,
        int maxLength = 100,
        int minRemove = 0,
        int maxRemove = 10,
        int minInsert = 0,
        int maxInsert = 10) => String(charset, new SequenceGenerationSettings()
        {
            MinLength = minLength,
            MaxLength = maxLength,
            MinRemove = minRemove,
            MaxRemove = maxRemove,
            MinInsert = minInsert,
            MaxInsert = maxInsert,
        });

    public static IGenerator<string> Newline() => Pick("\r", "\n", "\r\n");
}
