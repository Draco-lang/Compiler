using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Dap.Model;

internal sealed class RequestMessage
{
    [JsonPropertyName("seq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int SequenceNumber { get; set; }

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => "request";

    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Command { get; set; }

    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? Arguments { get; set; }
}
