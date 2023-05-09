using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Draco.Lsp.Model;

namespace Draco.Lsp.Serialization;

/// <summary>
/// Converter for <see cref="DocumentUri"/>.
/// </summary>
internal sealed class DocumentUriConverter : JsonConverter<DocumentUri>
{
    public override DocumentUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Uri.UnescapeDataString(reader.GetString() ?? string.Empty));

    public override DocumentUri ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => new(Uri.UnescapeDataString(reader.GetString() ?? string.Empty));

    public override void Write(Utf8JsonWriter writer, DocumentUri value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DocumentUri value, JsonSerializerOptions options)
        => writer.WritePropertyName(value.ToString());
}
