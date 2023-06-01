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
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

using DapMessage = Draco.Dap.Model.OneOf<Draco.Dap.Model.RequestMessage, Draco.Dap.Model.EventMessage, Draco.Dap.Model.ResponseMessage>;

namespace Draco.Dap.Adapter;

public sealed class DebugAdapterConnection
{
    private readonly IDuplexPipe transport;
    private readonly Dictionary<string, DebugAdapterMethodHandler> methodHandlers = new();

    private readonly TransformManyBlock<IAsyncEnumerable<DapMessage>, DapMessage> messageParser;
    private readonly ActionBlock<DapMessage> outgoingMessages;

    private readonly CancellationTokenSource shutdownTokenSource = new();

    private int lastMessageSeq = 0;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> pendingIncomingRequests = new();

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        Converters = { },
    };

    public DebugAdapterConnection(IDuplexPipe transport)
    {
        this.transport = transport;
    }

    private int NextSeq() => Interlocked.Increment(ref this.lastMessageSeq);

    public async Task ListenAsync()
    {
        // TODO: Temporary
        await foreach (var msg in this.DeserializeFromTransport())
        {
            var x = 0;
        }
    }

    private async IAsyncEnumerable<DapMessage> DeserializeFromTransport()
    {
        while (true)
        {
            DapMessage? message;

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
                yield return message.Value;
            }
            else
            {
                break;
            }
        }
    }

    private async Task<DapMessage?> ReadDapMessage()
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

    private static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, ref int contentLength, out DapMessage message)
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
                    message = JsonSerializer.Deserialize<DapMessage>(ref jsonReader, jsonOptions)!;
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

    private async Task SerializeToTransport(DapMessage message)
    {
        var writer = this.transport.Output;

        void WriteData()
        {
            var response = JsonSerializer.SerializeToUtf8Bytes(message, jsonOptions);

            var ContentLengthHeader = "Content-Length: "u8;
            var TwoNewLines = "\r\n\r\n"u8;

            var written = 0;
            var responseBuffer = writer.GetSpan(response.Length + ContentLengthHeader.Length + 14); // 14 UTF-8 bytes can encode any integer and 2 newlines

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
        var result = await writer.FlushAsync();
        if (result.IsCompleted)
        {
            this.outgoingMessages.Complete();
        }
    }

    private static readonly MethodInfo taskGetResult = typeof(Task<>).GetMethod("get_Result")!;

    private async Task<DapMessage?> ProcessRequestOrEvent(DapMessage message)
    {
        string method;
        JsonElement? @params;
        int? seq;

        if (message.Is<RequestMessage>(out var requestMessage))
        {
            method = requestMessage.Command;
            @params = requestMessage.Arguments;
            seq = requestMessage.SequenceNumber;
        }
        else if (message.Is<EventMessage>(out var eventMessage))
        {
            method = eventMessage.Event;
            @params = eventMessage.Body;
            seq = null;
        }
        else
        {
            throw new InvalidDataException();
        }

        // TODO: Handle custom messages like cancellation
        /*
        if (method.StartsWith("$/"))
        {
            return this.ProcessImplementationDefinedMethod(method, @params, seq);
        }
        */

        DapMessage? Error(ErrorResponse error)
        {
            // We can't respond to an event, even if we hit an error.
            if (seq is not null)
            {
                return this.CreateResponseMessage(seq.Value, method!, error);
            }

            return null;
        }

        if (!this.methodHandlers.TryGetValue(method, out var handler))
        {
            return Error(new()
            {
                Error = new()
                {
                    Id = -32601, // MethodNotFound
                    Format = "A handler for the method '{method}' was not registered.",
                    Variables = new Dictionary<string, string>()
                    {
                        { "method", method },
                    },
                }
            });
        }

        // If the params value exists, it must be an object or an array or the message is malformed JSON-RPC.
        if (@params is JsonElement { ValueKind: not (JsonValueKind.Object or JsonValueKind.Array) })
        {
            return Error(new()
            {
                Error = new()
                {
                    Id = -32600, // InvalidRequest
                    Format = "Invalid request.",
                }
            });
        }

        if (handler.ProducesResponse && seq is null)
        {
            // The client sent an event, but we have a request handler for this method.
            return null;
        }
        else if (!handler.ProducesResponse && seq is not null)
        {
            // The client sent a request, but we have an event handler for this method.
            return Error(new()
            {
                Error = new()
                {
                    Id = -32603,
                    Format = "A handler for the method '{method}' was registered as an event handler.",
                    Variables = new Dictionary<string, string>
                    {
                        { "method", method },
                    },
                },
            });
        }

        // Build the arguments we'll use when invoking the handler. The handler may take the deserialized params object
        // and/or a CancellationToken as arguments.
        var args = new List<object?>();
        if (handler.HasParams)
        {
            if (@params.HasValue)
            {
                try
                {
                    args.Add(@params.Value.Deserialize(handler.DeclaredParamsType, jsonOptions));
                }
                catch (JsonException ex)
                {
                    return Error(FromJsonException(ex));
                }
            }
            else
            {
                args.Add(null);
            }
        }

        CancellationTokenSource? cts = null;
        if (handler.HasCancellation)
        {
            var token = CancellationToken.None;

            if (handler.ProducesResponse)
            {
                // Only requests can be canceled, so we don't need to create a CancellationTokenSource for an event.
                cts = new CancellationTokenSource();
                this.pendingIncomingRequests.TryAdd(seq!.Value, cts);
                token = cts.Token;
            }

            // If the handler takes a CancellationToken, it is always the last argument.
            args.Add(token);
        }

        OneOf<object?, Exception> result;
        try
        {
            // Invoke the handler. Handlers always return a Task or Task<T>, so we await the returned task and extract its result if there is one.
            // We'll later serialize the result and send it back to the client as a response.
            var returnValue = (Task?)handler.HandlerMethod.Invoke(handler.Target, args.ToArray());
            await returnValue!;

            if (handler.DeclaredReturnType == typeof(Task))
            {
                result = (object?)null;
            }
            else
            {
                var getResult = (MethodInfo)returnValue.GetType().GetMemberWithSameMetadataDefinitionAs(taskGetResult);
                result = getResult.Invoke(returnValue, null);
            }
        }
        catch (Exception ex)
        {
            result = ex;
        }

        // If we created a CancellationTokenSource for this request, we need to remove it from the dictionary and dispose it
        if (cts is not null)
        {
            this.pendingIncomingRequests.Remove(seq!.Value, out _);
            cts.Dispose();
        }

        if (handler.ProducesResponse)
        {
            OneOf<object?, ErrorResponse> serializableResponse;
            if (result.Is<Exception>(out var error))
            {
                // Unwrap exceptions from the reflection invoke
                if (error is TargetInvocationException)
                {
                    error = error.InnerException;
                }

                var errorCode = error switch
                {
                    OperationCanceledException => -32800, // RequestCancelled
                    _ => -32603, // InternalError
                };

                serializableResponse = new ErrorResponse
                {
                    Error = new()
                    {
                        Id = errorCode,
                        Format = error?.Message ?? "Internal error while processing request.",
                    },
                };
            }
            else
            {
                serializableResponse = result.As<object?>();
            }

            return this.CreateResponseMessage(seq!.Value, method!, serializableResponse);
        }

        // No response
        return null;
    }

    private ResponseMessage CreateResponseMessage(int seq, string command, OneOf<object?, ErrorResponse> response)
    {
        var responseMessage = new ResponseMessage
        {
            Command = command,
            RequestSequenceNumber = seq,
            SequenceNumber = this.NextSeq(),
            Success = !response.Is<ErrorResponse>(),
        };

        if (response.Is<ErrorResponse>(out var error))
        {
            responseMessage.Body = JsonSerializer.SerializeToElement(error, jsonOptions);
            // NOTE: We don't set Message
            //   Contains the raw error in short form if `success` is false.
            //   This raw error might be interpreted by the client and is not shown in the
            //   UI.
        }
        else
        {
            responseMessage.Body = JsonSerializer.SerializeToElement(response.As<object>(), jsonOptions);
        }

        return responseMessage;
    }

    private static ErrorResponse FromJsonException(JsonException ex)
    {
        // Typically, only exceptions from Utf8JsonReader have the position info set.
        // So, we assume this is a parse error if it's there, and other errors are serialization errors.
        var code = ex.BytePositionInLine.HasValue
            ? -32700  // ParseError
            : -32602; // InvalidParams

        var responseError = new ErrorResponse
        {
            Error = new()
            {
                // NOTE: Not exact science
                Id = code,
                Format = ex.Message,
                ShowUser = true,
            },
        };

        return responseError;
    }

    public void Shutdown() => this.shutdownTokenSource.Cancel();
}
