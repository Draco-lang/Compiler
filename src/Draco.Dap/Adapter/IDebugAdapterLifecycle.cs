using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter;

internal interface IDebugAdapterLifecycle
{
    [Request("initialize")]
    public Task<InitializeResponse> InitializeAsync(InitializeRequestArguments args);
}
