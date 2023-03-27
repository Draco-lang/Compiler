using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OneOf;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Provides JSON serialization for the <see cref="IOneOf"/> types.
/// </summary>
public sealed class OneOfJsonConverter : JsonConverter
{
    private static Type? ExtractOneOfType(Type objectType)
    {
        // Unwrap, if nullable
        if (objectType.IsGenericType)
        {
            var genericType = objectType.GetGenericTypeDefinition();
            if (genericType != typeof(Nullable<>)) return null;
            objectType = objectType.GetGenericArguments()[0];
        }
        return objectType.IsAssignableTo(typeof(IOneOf))
            ? objectType
            : null;
    }

    public override bool CanConvert(Type objectType) => ExtractOneOfType(objectType) is not null;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
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
