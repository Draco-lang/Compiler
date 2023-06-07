using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Dap.Model;

internal sealed class EventMessage
{
    [JsonPropertyName("seq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int SequenceNumber { get; set; }

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => "event";

    [JsonPropertyName("event")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Event { get; set; }

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? Body { get; set; }
}
