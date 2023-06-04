using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Basic;

public interface IProcess
{
    [Request("launch", Mutating = true)]
    public Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args);

    [Request("attach", Mutating = true)]
    public Task<AttachResponse> AttachAsync(AttachRequestArguments args);

    [Request("threads")]
    public Task<ThreadsResponse> GetThreadsAsync();
}
