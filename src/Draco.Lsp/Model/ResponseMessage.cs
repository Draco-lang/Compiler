using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Lsp.Model;

internal sealed class ResponseMessage
{
    [JsonPropertyName("jsonrpc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Jsonrpc { get; set; }

    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required OneOf<int, string>? Id { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Result { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ResponseError? Error { get; set; }
}
