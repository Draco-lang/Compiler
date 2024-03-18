using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal class DisposeAction(Action action) : IDisposable
{
    public void Dispose() => action();
}
