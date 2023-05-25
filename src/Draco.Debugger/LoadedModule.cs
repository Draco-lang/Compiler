using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a module loaded by the runtime.
/// </summary>
internal sealed class LoadedModule
{
    /// <summary>
    /// The native representation of the module.
    /// </summary>
    public CorDebugModule CorDebugModule { get; }

    /// <summary>
    /// The name of this module.
    /// </summary>
    public string Name => this.CorDebugModule.Name;

    public LoadedModule(CorDebugModule corDebugModule)
    {
        this.CorDebugModule = corDebugModule;
    }
}
