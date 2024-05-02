using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class DisposeAction(Action action) : IDisposable
{
    public void Dispose() => action();
}
