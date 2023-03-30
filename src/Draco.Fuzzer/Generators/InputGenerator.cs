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
        string? charset,
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
