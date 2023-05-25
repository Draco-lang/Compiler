using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Debugger.IO;

/// <summary>
/// A stream reading from a memory address.
/// </summary>
internal sealed class MemoryAddressStream : Stream
{
    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length { get; }

    public override long Position
    {
        get => this.position;
        set
        {
            if (value < 0 || value > this.Length) throw new ArgumentOutOfRangeException(nameof(value));
            this.position = value;
        }
    }

    private readonly IntPtr address;
    private long position;

    public MemoryAddressStream(IntPtr address, long length)
    {
        this.address = address;
        this.Length = length;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => this.position + offset,
            SeekOrigin.End => this.Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        this.Position = newPosition;
        return this.Position;
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        var remainingLength = (int)(this.Length - this.Position);
        var currentAddress = new IntPtr(this.address + this.Position);
        var toCopy = Math.Min(remainingLength, count);
        Marshal.Copy(source: currentAddress, destination: buffer, startIndex: offset, length: toCopy);
        this.position += toCopy;
        return toCopy;
    }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
