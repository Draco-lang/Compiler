using System.Text.Json.Serialization;

namespace Draco.Lsp.Model;

/// <summary>
/// Represents information on a file/folder delete.
/// 
/// @since 3.16.0
/// </summary>
public sealed class FileDelete
{
    /// <summary>
    /// A file:// URI for the location of the file/folder being deleted.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [JsonRequired]
    public required System.Uri Uri { get; set; }
}
