using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Draco.Lsp.Model;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Since STJ doesn't support interface deserialization and the model uses them extensively,
/// we use a similar trick as OneOf.
/// We collect out a bunch of discriminating fields for each implementation.
/// </summary>
internal sealed class ModelInterfaceConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
           typeToConvert.IsInterface
        && typeToConvert.Namespace == typeof(RequestMessage).Namespace;
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter?)Activator.CreateInstance(typeof(ModelInterfaceConverter<>).MakeGenericType(typeToConvert));
}

internal sealed class ModelInterfaceConverter<TInterface> : JsonConverter<TInterface>
{
    /// <summary>
    /// Represents a single discrimination.
    /// </summary>
    /// <param name="Type">The type being discriminated.</param>
    /// <param name="Properties">The properties that exclude the use of <paramref name="Type"/>.</param>
    private readonly record struct Discrimination(Type Type, ImmutableHashSet<string> Properties);

    private static readonly Dictionary<(Type, JsonSerializerOptions), ImmutableArray<Discrimination>> discriminatorCache = new();

    private static ImmutableArray<Discrimination> GetDiscriminators(Type interfaceType, JsonSerializerOptions options)
    {
        // Check if it's cached
        if (discriminatorCache.TryGetValue((interfaceType, options), out var existing)) return existing;

        // No, we need to build the discriminators
        var implementors = typeof(RequestMessage).Assembly
            .GetTypes()
            .Where(t => t.Namespace == typeof(RequestMessage).Namespace)
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Contains(interfaceType));
        var builder = ImmutableArray.CreateBuilder<Discrimination>();

        // First, we collect all of the names of all one of types

        JsonTypeInfo JsonType(Type t) => options.TypeInfoResolver!.GetTypeInfo(t, options)!;

        var allPropertyNames = implementors
            .SelectMany(arg => JsonType(arg).Properties.Select(p => p.Name))
            .ToHashSet();

        // Next, we collect discriminators for each arg
        foreach (var implementor in implementors)
        {
            // Get the property names of this argument
            var argPropertyNames = JsonType(implementor)
                .Properties
                .Select(p => p.Name)
                .ToHashSet();

            // subtracting these names from all possible property names gives a discriminative set
            var discriminativeSet = allPropertyNames
                .Except(argPropertyNames)
                .ToImmutableHashSet();

            // Add to result
            builder.Add(new(implementor, discriminativeSet));
        }

        // Sort by most discriminators first
        builder.Sort((a, b) => b.Properties.Count - a.Properties.Count);

        // Build, save to cache
        var result = builder.ToImmutable();
        discriminatorCache.Add((interfaceType, options), result);

        // Done
        return result;
    }

    public override TInterface? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonElement.ParseValue(ref reader);
        // Get the discriminative fields
        var discriminators = GetDiscriminators(typeToConvert, options);

        // Go through all discriminators
        foreach (var (altType, discriminativeProps) in discriminators)
        {
            // If the object has the property, we discriminate
            var discriminate = discriminativeProps.Any(p => obj.TryGetProperty(p, out _));
            if (discriminate) continue;

            // This alternative matches, use it
            return (TInterface?)obj.Deserialize(altType, options);
        }

        // Unknown
        throw new InvalidOperationException();
    }

    public override void Write(Utf8JsonWriter writer, TInterface value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, options);
}
