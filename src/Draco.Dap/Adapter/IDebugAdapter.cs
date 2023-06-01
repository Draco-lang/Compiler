using System;
using System.Threading.Tasks;
using Draco.Dap.Adapter.Basic;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter;

/// <summary>
/// The interface that debg adapters need to implement.
/// </summary>
public interface IDebugAdapter
    : IDisposable
    , IProcessLifecycle
    , IBreakpoints
    , ISingleStepping
{
    // NOTE: This is handled by the lifecycle manager, so it's not annotated
    // The lifecycle manager will dynamically register capabilities here,
    // then invokes these methods
    public Task InitializeAsync(InitializeRequestArguments args);
}
