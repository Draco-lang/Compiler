using System.ComponentModel;

namespace Draco.Compiler.Tests.Utilities;

internal sealed class ReadOnlyMemoryStream : Stream
{
    private readonly ReadOnlyMemory<byte> _memory;

    public ReadOnlyMemoryStream(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _memory.Length;

    private int _position;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > _memory.Length)
                throw new ArgumentOutOfRangeException(nameof(value));

            _position = (int)value; // conversion would succeed, no need for checked context
        }
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        if (Position == _memory.Length)
            return 0;
        else if (Position > _memory.Length)
            throw new InvalidOperationException();
        else
        {
            var bytesRead = Math.Min(_memory.Length - _position, buffer.Length);
            _memory.Span.Slice(_position, bytesRead).CopyTo(buffer);
            _position += bytesRead;
            return bytesRead;
        }
    }

    public override int ReadByte()
    {
        if (Position == _memory.Length)
            return -1;
        else if (Position > _memory.Length)
            throw new InvalidOperationException();
        else
        {
            _position++;
            return _memory.Span[_position - 1];
        }
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(Read(buffer.AsSpan(offset, count)));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(Read(buffer.Span));
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        return origin switch
        {
            SeekOrigin.Begin => Position = offset,
            SeekOrigin.Current => Position += offset,
            SeekOrigin.End => Position = Length + offset,
            _ => throw new InvalidEnumArgumentException(nameof(origin), (int)origin, typeof(SeekOrigin)),
        };
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
