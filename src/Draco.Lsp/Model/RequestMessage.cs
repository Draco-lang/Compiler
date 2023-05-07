using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Lsp.Model;

internal sealed class RequestMessage
{
    [JsonPropertyName("jsonrpc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Jsonrpc { get; set; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required OneOf<int, string> Id { get; set; }

    [JsonPropertyName("method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Method { get; set; }

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? Params { get; set; }
}
