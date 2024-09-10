using static Draco.SourceGeneration.TemplateUtils;

namespace Draco.SourceGeneration.OneOf;

internal static class Template
{
    public static string Generate(Config config) => FormatCSharp($$"""
using System;
using System.Diagnostics.CodeAnalysis;

namespace {{config.RootNamespace}};

#nullable enable

/// <summary>
/// Interface for all one-of DUs.
/// </summary>
public interface IOneOf
{
    /// <summary>
    /// The stored value.
    /// </summary>
    public object? Value { get; }
}

{{For(1..config.MaxCases, nCases => $$"""
    /// <summary>
    /// A discriminated union implementation for {{nCases}} case(s).
    /// </summary>
    public readonly struct OneOf<{{For(1..nCases, ", ", i => $"T{i}")}}> : IOneOf
    {
        object? IOneOf.Value => this.index switch
        {
            {{For(1..nCases, i => $"{i} => this.field{i},")}}
            _ => throw new InvalidOperationException(),
        };

        private readonly byte index;
        {{For(1..nCases, i => $"private readonly T{i} field{i};")}}

        private OneOf(byte index, {{For(1..nCases, ", ", i => $"T{i} field{i}")}})
        {
            this.index = index;
            {{For(1..nCases, i => $"this.field{i} = field{i};")}}
        }

        {{For(1..nCases, i => $$"""
            public OneOf(T{{i}} value)
                : this({{i}}, {{For(1..nCases, ", ", j => i == j ? "value" : "default!")}})
            {
            }
        """)}}

        {{For(1..nCases, i => $$"""
            public static implicit operator OneOf<{{For(1..nCases, ", ", i => $"T{i}")}}>(T{{i}} value) => new(value);
        """)}}

        public T As<T>() => this.Is<T>(out var value)
            ? value
            : throw new InvalidCastException();
        
        public bool Is<T>() => this.Is<T>(out _);

        public bool Is<T>([MaybeNullWhen(false)] out T value)
        {
            {{For(1..nCases, i => $$"""
                if (typeof(T) == typeof(T{{i}}))
                {
                    if (this.index == {{i}})
                    {
                        value = (T)(object)this.field{{i}}!;
                        return true;
                    }
                    else
                    {
                        value = default;
                        return false;
                    }
                }
            """)}}
            value = default;
            return false;
        }

        public override string? ToString() => (this as IOneOf).Value?.ToString();
    }
""")}}

#nullable restore
""");
}
