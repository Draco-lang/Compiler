using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Provides JSON serialization for the <see cref="IOneOf"/> types.
/// </summary>
public sealed class OneOfJsonConverter : JsonConverter
{
    private static Type? ExtractOneOfType(Type objectType)
    {
        // Unwrap, if nullable
        objectType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return objectType.IsAssignableTo(typeof(IOneOf))
            ? objectType
            : null;
    }

    private static ImmutableArray<KeyValuePair<Type, ImmutableHashSet<string>>> GetDiscriminators(Type oneOfType)
    {
        var oneOfArgs = oneOfType.GetGenericArguments();
        var builder = ImmutableArray.CreateBuilder<KeyValuePair<Type, ImmutableHashSet<string>>>();

        // First, we collect all of the names of all one of types
        var allPropertyNames = oneOfArgs
            .SelectMany(arg => arg.GetProperties().Select(p => p.Name))
            .ToHashSet();

        // Next, we collect discriminators for each arg
        foreach (var arg in oneOfArgs)
        {
            // We don't care about primitives
            if (arg.IsPrimitive) continue;

            // Get the property names of this argument
            var argPropertyNames = arg
                .GetProperties()
                .Select(p => p.Name)
                .ToHashSet();

            // subtracting these names from all possible property names gives a discriminative set
            var disctiminativeSet = allPropertyNames
                .Except(argPropertyNames)
                .ToImmutableHashSet();

            // Add to result
            builder.Add(new(arg, disctiminativeSet));
        }

        // Sort by most discriminators first
        builder.Sort((a, b) => b.Value.Count - a.Value.Count);

        // Done
        return builder.ToImmutable();
    }

    public override bool CanConvert(Type objectType) => ExtractOneOfType(objectType) is not null;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var oneOfType = ExtractOneOfType(objectType);
        if (oneOfType is null) throw new InvalidOperationException();

        // Primitive type
        if (reader.ValueType?.IsPrimitive ?? false) return Activator.CreateInstance(oneOfType, reader.Value);

        // It's an object
        var obj = JObject.Load(reader);
        // Get the discriminative fields
        var discriminators = GetDiscriminators(oneOfType);

        // Go through all discriminators
        foreach (var (altType, discriminativeProps) in discriminators)
        {
            // If the object has the property, we discriminate
            var discriminate = discriminativeProps.Any(obj.ContainsKey);
            if (discriminate) continue;

            // This alternative matches, use it
            var alt = Activator.CreateInstance(altType);
            serializer.Populate(obj.CreateReader(), alt!);

            // Wrap it up in the one-of
            return Activator.CreateInstance(oneOfType, alt);
        }

        // Unknown
        throw new InvalidOperationException();
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            serializer.Serialize(writer, null);
            return;
        }
        var oneOf = (IOneOf)value;
        serializer.Serialize(writer, oneOf.Value);
    }
}
