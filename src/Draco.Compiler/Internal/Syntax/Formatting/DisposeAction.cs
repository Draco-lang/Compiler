using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

public sealed class DisposeAction(Action action) : IDisposable
{
    public void Dispose() => action();
}
