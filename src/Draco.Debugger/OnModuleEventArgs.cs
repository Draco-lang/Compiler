using System;

namespace Draco.Debugger;
public sealed class OnModuleEventArgs : EventArgs
{
    /// <summary>
    /// The module that was loaded/unloaded.
    /// </summary>
    public required Module Module { get; init; }
}
