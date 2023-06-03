using System.Text.Json;
using System.Text.Json.Serialization;

namespace Draco.Dap.Model;

internal static class ResponseMessages
{
    /// <summary>
    /// The request was cancelled.
    /// </summary>
    public const string Cancelled = "cancelled";

    /// <summary>
    /// The request may be retried once the adapter is in a 'stopped' state.
    /// </summary>
    public const string NotStopped = "notStopped";
}

internal sealed class ResponseMessage
{
    [JsonPropertyName("seq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int SequenceNumber { get; set; }

    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => "response";

    [JsonPropertyName("request_seq")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required int RequestSequenceNumber { get; set; }

    [JsonPropertyName("success")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required bool Success { get; set; }

    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public required string Command { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Message { get; set; }

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? Body { get; set; }
}
