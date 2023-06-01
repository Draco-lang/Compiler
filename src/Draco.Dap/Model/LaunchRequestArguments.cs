using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Draco.Dap.Model;

// NOTE: Custom because of LaunchAttributes
/// <summary>
/// Arguments for `launch` request. Additional attributes are implementation specific.
/// </summary>
public class LaunchRequestArguments
{
    /// <summary>
    /// If true, the launch request should launch the program without enabling debugging.
    /// </summary>
    [JsonPropertyName("noDebug")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? NoDebug { get; set; }

    /// <summary>
    /// Arbitrary data from the previous, restarted session.
    /// The data is sent as the `restart` attribute of the `terminated` event.
    /// The client should leave the data intact.
    /// </summary>
    [JsonPropertyName("__restart")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement? __Restart { get; set; }

    /// <summary>
    /// Implementation-specific launch attributes.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? LaunchAttributes { get; set; }
}
