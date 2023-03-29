using Draco.Lsp.Model;
using Newtonsoft.Json;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Converter for <see cref="DocumentUri"/>.
/// </summary>
internal sealed class DocumentUriConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(DocumentUri);
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
        new DocumentUri((string)reader.Value!);
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        writer.WriteValue(((DocumentUri)value!).ToString());
}
