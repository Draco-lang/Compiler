using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public static IGenerator<int> Integer(int min, int max)
    {
        var rnd = new Random();
        return Delegate(() => rnd.Next(min, max));
    }

    public static IGenerator<string> String(
        string? charset = null,
        SequenceGenerationSettings? settings = null)
    {
        var charGenerator = new CharGenerator()
        {
            Charset = charset ?? Charsets.Ascii,
        };
        var sequence = charGenerator.Sequence(settings);
        return sequence.Map(seq => new string(seq.ToArray()));
    }

    public static IGenerator<TNew> Map<TOld, TNew>(
        this IGenerator<TOld> generator,
        Func<TOld, TNew> map,
        Func<TNew, string>? toString = null)
    {
        toString ??= x => x?.ToString() ?? "null";
        return new MapGenerator<TOld, TNew>(generator, map, toString);
    }

    public static IGenerator<ImmutableArray<T>> Sequence<T>(
        this IGenerator<T> element,
        SequenceGenerationSettings? settings = null)
    {
        var generator = new SequenceGenerator<T>(element, settings);
        return generator;
    }
}
