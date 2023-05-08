using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Draco.Lsp.Model;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Provides JSON serialization for the <see cref="IOneOf"/> types.
/// </summary>
internal sealed class OneOfConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableTo(typeof(IOneOf));
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter?)Activator.CreateInstance(typeof(OneOfConverter<>).MakeGenericType(typeToConvert));
}

internal sealed class OneOfConverter<TOneOf> : JsonConverter<TOneOf>
    where TOneOf : IOneOf
{
    /// <summary>
    /// Represents a single discrimination.
    /// </summary>
    /// <param name="Type">The type being discriminated.</param>
    /// <param name="Properties">The properties that exclude the use of <paramref name="Type"/>.</param>
    private readonly record struct Discrimination(Type Type, ImmutableHashSet<string> Properties);

    private static readonly Dictionary<(Type, JsonSerializerOptions), ImmutableArray<Discrimination>> discriminatorCache = new();

    private static ImmutableArray<Discrimination> GetDiscriminators(Type oneOfType, JsonSerializerOptions options)
    {
        // Check if it's cached
        if (discriminatorCache.TryGetValue((oneOfType, options), out var existing)) return existing;

        // No, we need to build the discriminators
        var oneOfArgs = oneOfType.GetGenericArguments();
        var builder = ImmutableArray.CreateBuilder<Discrimination>();

        // First, we collect all of the names of all one of types

        JsonTypeInfo JsonType(Type t) => options.TypeInfoResolver!.GetTypeInfo(t, options)!;

        var allPropertyNames = oneOfArgs
            .SelectMany(arg => JsonType(arg).Properties.Select(p => p.Name))
            .ToHashSet();

        // Next, we collect discriminators for each arg
        foreach (var arg in oneOfArgs)
        {
            // We don't care about primitives
            if (arg.IsPrimitive) continue;

            // Get the property names of this argument
            var argPropertyNames = JsonType(arg)
                .Properties
                .Select(p => p.Name)
                .ToHashSet();

            // subtracting these names from all possible property names gives a discriminative set
            var discriminativeSet = allPropertyNames
                .Except(argPropertyNames)
                .ToImmutableHashSet();

            // Add to result
            builder.Add(new(arg, discriminativeSet));
        }

        // Sort by most discriminators first
        builder.Sort((a, b) => b.Properties.Count - a.Properties.Count);

        // Build, save to cache
        var result = builder.ToImmutable();
        discriminatorCache.Add((oneOfType, options), result);

        // Done
        return result;
    }

    private static Type? ExtractOneOfType(Type objectType)
    {
        // Unwrap, if nullable
        objectType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return objectType.IsAssignableTo(typeof(IOneOf))
            ? objectType
            : null;
    }

    private static bool IsPrimitive(JsonTokenType token) => token
        is JsonTokenType.True
        or JsonTokenType.False
        or JsonTokenType.Number
        or JsonTokenType.String
        or JsonTokenType.Null;

    public override bool CanConvert(Type objectType) => ExtractOneOfType(objectType) is not null;

    private static object? FromPrimitive(ref Utf8JsonReader reader) => reader.TokenType switch
    {
        JsonTokenType.String => reader.GetString(),
        JsonTokenType.Number => reader.GetInt32(),
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        JsonTokenType.Null => null,
        _ => throw new ArgumentException("Reader was in an invalid state.", nameof(reader))
    };

    public override TOneOf? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var oneOfType = ExtractOneOfType(typeToConvert)
                     ?? throw new InvalidOperationException();

        // Primitive type
        if (IsPrimitive(reader.TokenType)) return (TOneOf?)Activator.CreateInstance(oneOfType, FromPrimitive(ref reader));

        // It could be a tuple
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var array = JsonElement.ParseValue(ref reader);
            var tupleVariant = oneOfType
                .GetGenericArguments()
                .Single(t => t.IsAssignableTo(typeof(ITuple)));
            return (TOneOf?)Activator.CreateInstance(oneOfType, array.Deserialize(tupleVariant, options));
        }


        // Assume it's an object
        var obj = JsonElement.ParseValue(ref reader);
        // Get the discriminative fields
        var discriminators = GetDiscriminators(oneOfType, options);

        // Go through all discriminators
        foreach (var (altType, discriminativeProps) in discriminators)
        {
            // If the object has the property, we discriminate
            var discriminate = discriminativeProps.Any(p => obj.TryGetProperty(p, out _));
            if (discriminate) continue;

            // This alternative matches, use it
            var alt = obj.Deserialize(altType, options);

            // Wrap it up in the one-of
            return (TOneOf?)Activator.CreateInstance(oneOfType, alt);
        }

        // Unknown
        throw new InvalidOperationException();
    }

    public override void Write(Utf8JsonWriter writer, TOneOf value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
