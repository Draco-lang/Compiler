using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger;
public sealed class OnModuleEventArgs : EventArgs
{
    /// <summary>
    /// The module that was loaded/unloaded.
    /// </summary>
    public required Module Module { get; init; }
}
