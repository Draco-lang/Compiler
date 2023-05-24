using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// A cross-platform <see cref="DbgShim"/> implementation.
/// </summary>
internal sealed class XplatDbgShim : DbgShim, IDisposable
{
    private bool disposedValue;

    public XplatDbgShim(IntPtr hModule)
        : base(hModule)
    {
    }

    protected override T GetDelegate<T>(string procName)
    {
        var procAddress = NativeLibrary.GetExport(this.hModule, procName);
        if (procAddress == IntPtr.Zero)
        {
            throw new InvalidOperationException($"failed to get address of procedure '{procName}': {(HRESULT)Marshal.GetLastPInvokeError()}");
        }

        return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
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
