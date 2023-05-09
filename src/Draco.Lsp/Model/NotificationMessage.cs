using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Lsp.Model;

internal sealed class NotificationMessage
{
    [JsonPropertyName("jsonrpc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Jsonrpc { get; set; }

    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Method { get; set; }

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? Params { get; set; }
}
