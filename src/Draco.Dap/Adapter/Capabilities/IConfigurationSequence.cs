using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Capabilities;

public interface IConfigurationSequence
{
    [Capability(nameof(Model.Capabilities.SupportsConfigurationDoneRequest))]
    public bool SupportsConfigurationDoneRequest => true;

    [Request("configurationDone")]
    public Task<ConfigurationDoneResponse> ConfigurationDoneAsync(ConfigurationDoneArguments args);
}
