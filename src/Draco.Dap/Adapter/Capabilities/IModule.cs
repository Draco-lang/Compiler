using Draco.Dap.Attributes;

namespace Draco.Dap.Adapter.Capabilities;

public interface IModule
{
    [Capability(nameof(Model.Capabilities.SupportsModulesRequest))]
    public bool SupportsModulesRequest => true;
}
