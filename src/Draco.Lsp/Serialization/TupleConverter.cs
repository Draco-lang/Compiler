using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Serializes tuples into JSON arrays and back.
/// </summary>
internal sealed class TupleConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType.IsAssignableTo(typeof(ITuple));

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        var array = JArray.Load(reader);
        var tupleElementTypes = objectType.GetGenericArguments();
        var tupleElements = array
            .Zip(tupleElementTypes)
            .Select(pair => pair.First.ToObject(pair.Second, serializer))
            .ToArray();
        return Activator.CreateInstance(objectType, tupleElements);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        var tuple = (ITuple)value;
        writer.WriteStartArray();
        for (var i = 0; i < tuple.Length; ++i) serializer.Serialize(writer, tuple[i]);
        writer.WriteEndArray();
    }
}
