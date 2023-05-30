using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Draco.Dap.Model;

namespace Draco.Dap.Serialization;

/// <summary>
/// Converts <see cref="ProtocolMessage"/> types.
/// </summary>
internal sealed class ProtocolMessageConverter : JsonConverter<ProtocolMessage>
{
    private static string? ProtocolMessageNamespace { get; } = typeof(ProtocolMessage).Namespace;
    private static IReadOnlyDictionary<string, Type> ProtocolMessageTypes { get; } = typeof(ProtocolMessage)
        .Assembly
        .GetTypes()
        .Where(t => !t.IsNested)
        .Where(t => t.Namespace == ProtocolMessageNamespace)
        .ToDictionary(t => t.Name, StringComparer.InvariantCultureIgnoreCase);

    public override ProtocolMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        var messageType = DetermineMessageType(element);
        return (ProtocolMessage?)element.Deserialize(messageType, options);
    }

    public override void Write(Utf8JsonWriter writer, ProtocolMessage value, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    private static Type DetermineMessageType(JsonElement element)
    {
        var messageType = element.GetProperty("type").GetString()!;
        switch (messageType)
        {
        case "request":
        {
            var command = element.GetProperty("command").GetString()!;
            return ProtocolMessageTypes[$"{command}Request"];
        }
        case "event":
        {
            var @event = element.GetProperty("event").GetString()!;
            return ProtocolMessageTypes[$"{@event}Event"];
        }
        default:
            throw new NotSupportedException($"message type {messageType} is not supported");
        }
    }
}
