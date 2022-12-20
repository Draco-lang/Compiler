using System.Text.Json.Serialization;

namespace Draco.Editor.Web;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OnInit))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
