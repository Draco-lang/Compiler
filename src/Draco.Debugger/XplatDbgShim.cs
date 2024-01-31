using System;
using System.Runtime.InteropServices;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// A cross-platform <see cref="DbgShim"/> implementation.
/// </summary>
internal sealed class XplatDbgShim : DbgShim, IDisposable
{
    private bool disposedValue;
    private readonly nint hModule;

    public XplatDbgShim(IntPtr hModule)
        : base(hModule)
    {
        this.hModule = hModule;
    }

    private void DisposeImpl()
    {
        if (this.disposedValue) return;

        NativeLibrary.Free(this.hModule);
        this.disposedValue = true;
    }

    ~XplatDbgShim()
    {
        this.DisposeImpl();
    }

    public void Dispose()
    {
        this.DisposeImpl();
        GC.SuppressFinalize(this);
    }
}
