using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Lsp.Serialization;

internal sealed class ResponseError
{
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int Code { get; set; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Message { get; set; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? Data { get; set; }
}
