using System.Buffers.Text;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Channels;

namespace Draco.JsonRpc;

/// <summary>
/// A base class for a JSON-RPC connection.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public abstract class JsonRpcConnection<TMessage>
{
    /// <summary>
    /// The IO transport pipeline to send and receive messages through.
    /// </summary>
    public abstract IDuplexPipe Transport { get; }

    /// <summary>
    /// The options to serialize messages with.
    /// </summary>
    public abstract JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// The options to deserialize messages with.
    /// </summary>
    public abstract JsonSerializerOptions JsonDeserializerOptions { get; }

    // Reader -> processor
    private readonly Channel<TMessage> incomingMessages = Channel.CreateUnbounded<TMessage>(new()
    {
        SingleReader = true,
        SingleWriter = true,
    });
    // Anything sending out -> writer
    private readonly Channel<TMessage> outgoingMessages = Channel.CreateUnbounded<TMessage>(new()
    {
        SingleReader = true,
        SingleWriter = false,
    });

    // TODO: Doc
    public Task Listen() => Task.WhenAll(
        this.ReaderLoopAsync(),
        this.WriterLoopAsync(),
        this.ProcessorLoopAsync());

    #region Message Loops
    private async Task ReaderLoopAsync()
    {
        while (true)
        {
            var (message, foundMessage) = await this.ReadMessageAsync();
            if (!foundMessage) break;

            await this.incomingMessages.Writer.WriteAsync(message!);
        }
    }

    private async Task WriterLoopAsync()
    {
        await foreach (var message in this.outgoingMessages.Reader.ReadAllAsync())
        {
            await this.WriteMessageAsync(message);
        }
    }

    private async Task ProcessorLoopAsync()
    {
        static bool MutatesWorkspace(TMessage message) => throw new NotImplementedException();

        var currentTasks = new List<Task>();
        await foreach (var message in this.incomingMessages.Reader.ReadAllAsync())
        {
            if (MutatesWorkspace(message))
            {
                await Task.WhenAll(currentTasks);
                currentTasks.Clear();

                await this.ProcessMessageAsync(message);
            }
            else
            {
                currentTasks.Add(this.ProcessMessageAsync(message));
            }
        }

        await Task.WhenAll(currentTasks);
    }
    #endregion

    protected Task ProcessMessageAsync(TMessage message)
    {
        // TODO
    }

    #region Serialization
    private async Task<(TMessage? Message, bool Found)> ReadMessageAsync()
    {
        var contentLength = -1;
        var reader = this.Transport.Input;

        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            var foundJson = this.TryParseMessage(ref buffer, ref contentLength, out var message);

            if (result.IsCompleted || result.IsCanceled)
            {
                // The stream abruptly ended
                if (buffer.Length > 0) throw new InvalidDataException();

                break;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (foundJson) return (message, true);
        }

        return (default, false);
    }

    private bool TryParseMessage(
        ref ReadOnlySequence<byte> buffer,
        ref int contentLength,
        [MaybeNullWhen(false)] out TMessage message)
    {
        var reader = new SequenceReader<byte>(buffer);

        while (reader.TryReadTo(sequence: out var header, "\r\n"u8))
        {
            if (header.IsEmpty)
            {
                // We've just scanned past the final header. We're now ready to read the JSON payload.
                if (reader.TryReadExact(contentLength, out var utf8Json))
                {
                    buffer = buffer.Slice(reader.Position, reader.Position);

                    var jsonReader = new Utf8JsonReader(utf8Json);
                    message = JsonSerializer.Deserialize<TMessage>(ref jsonReader, this.JsonDeserializerOptions)!;
                    return true;
                }
                else
                {
                    // We couldn't read the full JSON payload, so we'll come back here when more data is available.
                    // Keep the \r\n right before the JSON payload. Otherwise, we'll never enter this loop again.
                    reader.Rewind(2);
                    buffer = buffer.Slice(reader.Position);
                    break;
                }
            }

            buffer = buffer.Slice(reader.Position);

            var headerText = header.IsSingleSegment ? header.FirstSpan : header.ToArray();

            var ContentLengthHeader = "Content-Length: "u8;
            var ContentTypeHeader = "Content-Type: "u8;

            if (headerText.StartsWith(ContentLengthHeader))
            {
                if (!Utf8Parser.TryParse(headerText[ContentLengthHeader.Length..], out contentLength, out _))
                {
                    throw new InvalidDataException();
                }
            }
            else if (headerText.StartsWith(ContentTypeHeader))
            {
                // In theory, we are supposed to ensure that the requested encoding is UTF-8.
            }
        }

        message = default;
        return false;
    }

    private ValueTask<FlushResult> WriteMessageAsync(TMessage message)
    {
        var writer = this.Transport.Output;

        void WriteData()
        {
            var response = JsonSerializer.SerializeToUtf8Bytes(message, this.JsonSerializerOptions);

            var ContentLengthHeader = "Content-Length: "u8;
            var TwoNewLines = "\r\n\r\n"u8;

            // 14 UTF-8 bytes can encode any integer and 2 newlines
            var written = 0;
            var responseBuffer = writer.GetSpan(response.Length + ContentLengthHeader.Length + 14);

            ContentLengthHeader.CopyTo(responseBuffer);
            written += ContentLengthHeader.Length;

            Utf8Formatter.TryFormat(response.Length, responseBuffer[written..], out var contentLengthLength);
            written += contentLengthLength;

            TwoNewLines.CopyTo(responseBuffer[written..]);
            written += TwoNewLines.Length;

            response.CopyTo(responseBuffer[written..]);
            written += response.Length;

            writer.Advance(written);
        }

        WriteData();
        return writer.FlushAsync();
    }
    #endregion
}
