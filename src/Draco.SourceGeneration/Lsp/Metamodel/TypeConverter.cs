using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.SourceGeneration.Lsp.Metamodel;

internal sealed class TypeConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonElement.ParseValue(ref reader);
        var typeKind = obj.GetProperty("kind").GetString();
        return typeKind switch
        {
            "base" or "reference" => obj.Deserialize<NamedType>(options),
            "array" => obj.Deserialize<ArrayType>(options),
            "map" => obj.Deserialize<MapType>(options),
            "or" or "and" or "tuple" => obj.Deserialize<AggregateType>(options),
            "literal" => obj.Deserialize<StructureLiteralType>(options),
            "stringLiteral" => obj.Deserialize<LiteralType>(options),
            _ => throw new NotSupportedException($"the type kind '{typeKind}' is not supported"),
        };
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) =>
        throw new NotSupportedException();
}
