using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Draco.Dap.Model;

// NOTE: Custom because of AttachAttributes
/// <summary>
/// Arguments for `launch` request. Additional attributes are implementation specific.
/// </summary>
public class AttachRequestArguments
{
    /// <summary>
    /// Arbitrary data from the previous, restarted session.
    /// The data is sent as the `restart` attribute of the `terminated` event.
    /// The client should leave the data intact.
    /// </summary>
    [JsonPropertyName("__restart")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? __Restart { get; set; }

    /// <summary>
    /// Implementation-specific attach attributes.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AttachAttributes { get; set; }
}
