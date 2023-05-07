using System.Text.Json;
using System.Text.Json.Serialization;
using Draco.Lsp.Model;

namespace Draco.Lsp.Serialization;

internal sealed class ResponseMessage
{
    [JsonPropertyName("jsonrpc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Jsonrpc { get; set; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required OneOf<int, string?> Id { get; set; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ResponseError? Error { get; set; }
}
