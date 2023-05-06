using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Serializes tuples into JSON arrays and back.
/// </summary>
///
internal sealed class TupleConverter : JsonConverter<ITuple>
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.FullName?.StartsWith("System.ValueTuple`") ?? false;

    public override ITuple? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        var array = JsonElement.ParseValue(ref reader).EnumerateArray();
        var tupleElementTypes = typeToConvert.GetGenericArguments();
        var tupleElements = array
            .Zip(tupleElementTypes)
            .Select(pair => pair.First.Deserialize(pair.Second, options))
            .ToArray();

        return (ITuple)Activator.CreateInstance(typeToConvert, tupleElements)!;
    }

    public override void Write(Utf8JsonWriter writer, ITuple value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartArray();
        for (var i = 0; i < value.Length; ++i) JsonSerializer.Serialize(writer, value[i], options);
        writer.WriteEndArray();
    }
}
