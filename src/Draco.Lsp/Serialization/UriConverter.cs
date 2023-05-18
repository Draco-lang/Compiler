using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Converter for <see cref="Uri"/>.
/// </summary>
internal sealed class UriConverter : JsonConverter<Uri>
{
    public override Uri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Uri.UnescapeDataString(reader.GetString() ?? string.Empty));

    public override Uri ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Uri.UnescapeDataString(reader.GetString() ?? string.Empty));

    public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

    public override void WriteAsPropertyName(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());
}
