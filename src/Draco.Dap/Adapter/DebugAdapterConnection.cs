using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Draco.Dap.Adapter;

public sealed class DebugAdapterConnection
{
    private readonly IDuplexPipe transport;

    public DebugAdapterConnection(IDuplexPipe transport)
    {
        this.transport = transport;
    }

    public async Task ListenAsync()
    {
        // TODO: Temporary
    }
}
