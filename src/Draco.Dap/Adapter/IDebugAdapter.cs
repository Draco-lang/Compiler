using System;
using Draco.Dap.Adapter.Basic;

namespace Draco.Dap.Adapter;

/// <summary>
/// The interface that debg adapters need to implement.
/// </summary>
public interface IDebugAdapter
    : IDisposable
    , IBreakpoints
    , ISingleStepping
{
}
