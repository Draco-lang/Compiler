using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Draco.SourceGeneration;

internal static class EquatableArray
{
    public static EquatableArray<T> AsEquatableArray<T>(this ImmutableArray<T> array)
        where T : IEquatable<T> => new(array);

    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> soruce)
        where T : IEquatable<T> => new(soruce.ToImmutableArray());
}

[JsonConverter(typeof(EquatableArrayConverter))]
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IList<T>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> array;

    public EquatableArray(ImmutableArray<T> array)
    {
        this.array = array;
    }

    public bool IsEmpty => this.array.IsEmpty;

    int ICollection<T>.Count => this.array.Length;

    public int Length => this.array.Length;

    public bool IsReadOnly => true;

    public ref readonly T this[int index] => ref this.array.ItemRef(index);

    T IList<T>.this[int index]
    {
        get => this[index];
        set => throw new NotSupportedException();
    }

    public bool Equals(EquatableArray<T> array) => this.array.AsSpan().SequenceEqual(array.array.AsSpan());

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EquatableArray<T> array && Equals(this, array);

    public override int GetHashCode() => ((IStructuralEquatable)this.array).GetHashCode(EqualityComparer<T>.Default);

    public T[] ToArray() => this.array.ToArray();

    public int IndexOf(T item) => this.array.IndexOf(item);

    public bool Contains(T item) => this.array.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => this.array.CopyTo(array, arrayIndex);

    public void Insert(int index, T item) => throw new NotSupportedException();

    public void RemoveAt(int index) => throw new NotSupportedException();

    public void Add(T item) => throw new NotSupportedException();

    public void Clear() => throw new NotSupportedException();

    public bool Remove(T item) => throw new NotSupportedException();

    public ImmutableArray<T>.Enumerator GetEnumerator() => this.array.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)this.array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.array).GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}

internal sealed class EquatableArrayConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(EquatableArray<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(EquatableArrayJsonConverter<>);
        var converter = (JsonConverter)Activator.CreateInstance(converterType.MakeGenericType(elementType))!;

        return converter;
    }

    private class EquatableArrayJsonConverter<T> : JsonConverter<EquatableArray<T>>
        where T : IEquatable<T>
    {
        public override EquatableArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException();
            }

            reader.Read();

            var elements = ImmutableArray.CreateBuilder<T>();

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                var value = JsonSerializer.Deserialize<T>(ref reader, options);
                elements.Add(value!);
                reader.Read();
            }

            return elements.ToImmutable().ToEquatableArray();
        }

        public override void Write(Utf8JsonWriter writer, EquatableArray<T> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, value.AsEnumerable(), options);
    }
}
