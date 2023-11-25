using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Draco.Compiler.Tests.Decompilation;

internal sealed class CilSpaceAgnosticStringComparer : IEqualityComparer<string>
{
    public static CilSpaceAgnosticStringComparer Ordinal { get; } = new(StringComparison.Ordinal);

    private readonly StringComparison _comparison;

    public CilSpaceAgnosticStringComparer(StringComparison comparison)
    {
        _comparison = comparison;
    }

    public bool Equals(string? x, string? y)
    {
        if (x is null)
            return y is null;

        if (y is null)
            return false;

        var xIt = new Enumerator(x);
        var yIt = new Enumerator(y);

        while (true)
            if (xIt.MoveNext())
            {
                if (!yIt.MoveNext())
                    return false;

                if (!xIt.CurrentSpan.Equals(yIt.CurrentSpan, _comparison))
                    return false;
            }
            else
                // one of them ended earlier
                return !yIt.MoveNext();
    }

    public int GetHashCode([DisallowNull] string obj)
    {
        var hash = new HashCode();

        var span = obj.AsSpan();

        foreach (var range in new Enumerable(span))
            hash.AddBytes(MemoryMarshal.AsBytes(span[range]));

        return hash.ToHashCode();
    }

    private readonly ref struct Enumerable
    {
        public ReadOnlySpan<char> String { get; }

        public Enumerable(ReadOnlySpan<char> @string) => String = @string;

        public Enumerator GetEnumerator() => new(String);
    }

    private ref struct Enumerator
    {
        public ReadOnlySpan<char> String { get; }

        private int _start;
        private int _end;

        public Enumerator(ReadOnlySpan<char> @string)
        {
            String = @string;
        }

        public bool MoveNext()
        {
            var s = String;

            _start = _end;

            while (_start < s.Length && IsWhiteSpace(s[_start]))
                _start++;

            _end = _start;

            if (_start == s.Length)
                return false;

            if (s[_end] is '\'' or '\"')
            {
                var quote = s[_end];
                _end++;

                while (s[_end] != quote && _end < s.Length)
                    _end++;

                if (_end == s.Length)
                    throw new InvalidOperationException("Unclosed quoted string");

                _end++;
            }
            else
                while (_end < s.Length && !IsWhiteSpace(s[_end]))
                    _end++;
            return true;
        }

        private static bool IsWhiteSpace(char ch)
        {
            // don't use char.IsWhiteSpace as it checks additional chars, which won't appear in code
            return ch is ' ' or '\n' or '\r';
        }

        public readonly ReadOnlySpan<char> CurrentSpan => String[Current];

        public readonly Range Current => new(_start, _end);
    }
}
