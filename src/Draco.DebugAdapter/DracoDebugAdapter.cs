using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Adapter;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IDebugAdapter
{
    private readonly IDebugClient client;

    public DracoDebugAdapter(IDebugClient client)
    {
        this.client = client;
    }

    public void Dispose() { }
}
