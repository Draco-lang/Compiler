using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;

namespace Draco.Dap.Adapter.Capabilities;

public interface IModule
{
    [Capability(nameof(Model.Capabilities.SupportsModulesRequest))]
    public bool SupportsModulesRequest => true;
}
