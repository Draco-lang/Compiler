using System.Collections.Generic;
using System;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Tasks;
using Draco.Dap.Model;
using System.Buffers.Text;
using System.Buffers;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Draco.Dap.Serialization;
using System.Reflection;

namespace Draco.Dap.Adapter;

public sealed class DebugAdapterConnection
{
    private readonly IDuplexPipe transport;
    private readonly Dictionary<string, DebugAdapterMethodHandler> methodHandlers = new();

    private readonly CancellationTokenSource shutdownTokenSource = new();

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        Converters = { new ProtocolMessageConverter() },
    };

    public DebugAdapterConnection(IDuplexPipe transport)
    {
        this.transport = transport;
    }

    public void AddRpcMethod(MethodInfo handlerMethod, object? target)
    {
        var handler = new DebugAdapterMethodHandler(handlerMethod, target);
        this.methodHandlers.Add(handler.MethodName, handler);
    }

    public async Task ListenAsync()
    {
        // TODO: Temporary
        await foreach (var msg in this.DeserializeFromTransport())
        {
            var x = 0;
        }
    }

    private async IAsyncEnumerable<ProtocolMessage> DeserializeFromTransport()
    {
        while (true)
        {
            ProtocolMessage? message;

            try
            {
                // We don't pass the CancellationToken all the way down to PipeReader because it has no effect
                // on standard I/O (the I/O itself cannot be canceled).    
                message = await this.ReadDapMessage().WaitAsync(this.shutdownTokenSource.Token);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == this.shutdownTokenSource.Token)
            {
                // If Shutdown is called, we do not want the dataflow network to end in a faulted state.
                break;
            }
            catch (JsonException ex)
            {
                // TODO
                throw new NotImplementedException();
                // If we're sent invalid JSON, we're not really sure what we're responding to.
                // According to the spec, we should send an error response with a null id.
                // this.outgoingMessages.Post(CreateResponseMessage(id: null, FromJsonException(ex)));
                continue;
            }

            if (message is not null)
            {
                yield return message;
            }
            else
            {
                break;
            }
        }
    }

    private async Task<ProtocolMessage?> ReadDapMessage()
    {
        var contentLength = -1;
        var reader = this.transport.Input;

        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            var foundJson = TryParseMessage(ref buffer, ref contentLength, out var message);

            if (result.IsCompleted || result.IsCanceled)
            {
                if (buffer.Length > 0)
                {
                    // The stream abruptly ended.
                    throw new InvalidDataException();
                }

                break;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (foundJson)
            {
                return message;
            }
        }

        return null;
    }

    private static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, ref int contentLength, [MaybeNullWhen(false)] out ProtocolMessage message)
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
                    message = JsonSerializer.Deserialize<ProtocolMessage>(ref jsonReader, jsonOptions)!;
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
}
