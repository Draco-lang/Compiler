using System;
using System.Threading.Tasks;
using Draco.Dap.Adapter.Basic;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter;

/// <summary>
/// The interface that debug adapters need to implement.
/// </summary>
public interface IDebugAdapter
    : IDisposable
    , IProcess
    , ISource
    , IBreakpoints
    , ISingleStepping
    , IProgramState
{
    // NOTE: This is handled by the lifecycle manager, so it's not annotated
    // The lifecycle manager will dynamically register capabilities here,
    // then invokes these methods
    public Task InitializeAsync(InitializeRequestArguments args);
}
