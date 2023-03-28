using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Converts enum values according to the attribute.
/// </summary>
internal sealed class EnumValueConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        objectType.GetCustomAttribute<EnumMemberAttribute>() is not null;

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
        reader.Value;

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        writer.WriteValue(value);
}
