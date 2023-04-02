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
internal static class InputGenerator
{
    private sealed class DelegateGenerator<T> : IInputGenerator<T>
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

    public static IInputGenerator<T> Delegate<T>(
        Func<T> nextEpoch,
        Func<T>? nextMutation = null,
        Func<T, string>? toString = null)
    {
        nextMutation ??= nextEpoch;
        toString ??= x => x?.ToString() ?? "null";
        return new DelegateGenerator<T>(nextEpoch, nextMutation, toString);
    }

    public static IInputGenerator<int> Integer(int min, int max)
    {
        var rnd = new Random();
        return Delegate(() => rnd.Next(min, max));
    }

    public static IInputGenerator<TNew> Map<TOld, TNew>(
        this IInputGenerator<TOld> generator,
        Func<TOld, TNew> map,
        Func<TNew, string>? toString = null)
    {
        toString ??= v => v?.ToString() ?? "null";
        return new MapGenerator<TOld, TNew>(generator, map, toString);
    }

    public static IInputGenerator<ImmutableArray<T>> Sequence<T>(
        this IInputGenerator<T> element,
        Action<SequenceGenerator<T>>? configure = null)
    {
        var generator = new SequenceGenerator<T>(element);
        configure?.Invoke(generator);
        return generator;
    }

    public static IInputGenerator<string> String(
        string? charset = null,
        Action<SequenceGenerator<char>>? configure = null)
    {
        var charGenerator = new CharGenerator()
        {
            Charset = charset ?? Charsets.Ascii,
        };
        var sequence = charGenerator.Sequence(configure);
        return sequence.Map(seq => new string(seq.ToArray()));
    }
}
