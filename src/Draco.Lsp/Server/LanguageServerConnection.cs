using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Draco.Lsp.Model;
using Draco.Lsp.Serialization;

using LspMessage = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RequestMessage, Draco.Lsp.Model.NotificationMessage, Draco.Lsp.Model.ResponseMessage>;

namespace Draco.Lsp.Server;

public sealed class LanguageServerConnection
{
    private readonly IDuplexPipe transport;
    private readonly Dictionary<string, LanguageServerMethodHandler> methodHandlers = new();

    private readonly TransformManyBlock<IAsyncEnumerable<LspMessage>, LspMessage> messageParser;
    private readonly ActionBlock<LspMessage> outgoingMessages;

    private readonly CancellationTokenSource shutdownTokenSource = new();

    private int lastMessageId = 0;
    private readonly ConcurrentDictionary<OneOf<int, string>, CancellationTokenSource> pendingIncomingRequests = new();

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        Converters =
        {
            new TupleConverter(),
            new UriConverter(),
        }
    };
    private static readonly JsonSerializerOptions jsonDeserializerOptions = new()
    {
        Converters =
        {
            new TupleConverter(),
            new UriConverter(),
            new ModelInterfaceConverter(),
        }
    };

    public LanguageServerConnection(IDuplexPipe pipeTransport)
    {
        this.transport = pipeTransport;

        // We create a dataflow network to handle the processing of messages from the LSP client.

        var scheduler = new ConcurrentExclusiveSchedulerPair();

        // When the server is started, we post DeserializeFromTransport() to this block.
        // This will start deserializing messages from the client and pushing them through the dataflow network.
        this.messageParser = new TransformManyBlock<IAsyncEnumerable<LspMessage>, LspMessage>(
            ae => ae,
            new()
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

        // Requests and notifications are sent to one of two blocks, the concurrent block or the exclusive block, based on whether the request processing
        // is expected to mutate the state of the workspace. These blocks are configured using ConcurrentExclusiveSchedulerPair so that messages sent
        // to the exclusive block are processed one at a time, in order, and never simultaneously with messages sent to the concurrent block. The concurrent block
        // will accept and process messages in parallel as long as no mutating messages are waiting to be processed.
        // This allows us to use parallel processing for non-mutating ("query") requests while ensuring that mutating requests are processed in the order that the
        // client sends them.
        //
        // (There are likely still race conditions that can lead to messages being processed in the wrong order, but someone would need
        // to study the code in TPL Dataflow to work out all the edge cases. However, it's clear that the current situation is better than allowing all requests
        // to execute in parallel, which corrupts the state of the language server very quickly.)
        var concurrentMessageHandler = new TransformBlock<LspMessage, LspMessage?>(
            this.ProcessRequestOrNotification,
            new()
            {
                TaskScheduler = scheduler.ConcurrentScheduler,
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                EnsureOrdered = false,
            });

        var exclusiveMessageHandler = new TransformBlock<LspMessage, LspMessage?>(
            this.ProcessRequestOrNotification,
            new()
            {
                TaskScheduler = scheduler.ExclusiveScheduler
                // By default, blocks execute messages in order and with no parallelism.
            });

        // ProcessRequestOrNotification returns a non-null result when a response message needs to be sent back to the client.
        // We keep only non-null responses and forward them to SerializeToTransport, which will serialize each LspMessage object it consumes to the output stream.
        var filterNullResponses = new TransformManyBlock<LspMessage?, LspMessage>(
            m => m.HasValue ? new[] { m.Value } : Array.Empty<LspMessage>());

        this.outgoingMessages = new ActionBlock<LspMessage>(this.SerializeToTransport!);

        bool ShouldHandle(LspMessage message, bool isExclusiveBlock)
        {
            string method;
            if (message.Is<RequestMessage>(out var request))
            {
                method = request.Method;
            }
            else if (message.Is<NotificationMessage>(out var notification))
            {
                method = notification.Method;
            }
            else // Response messages are handled in SendRequestAsync
            {
                return false;
            }

            if (this.methodHandlers.TryGetValue(method, out var handler))
            {
                return isExclusiveBlock == handler.Mutating;
            }

            // Handle in the concurrent block.
            return isExclusiveBlock == false;
        }

        var propCompletion = new DataflowLinkOptions { PropagateCompletion = true };

        this.messageParser.LinkTo(concurrentMessageHandler, propCompletion, m => ShouldHandle(m, isExclusiveBlock: false));
        this.messageParser.LinkTo(exclusiveMessageHandler, propCompletion, m => ShouldHandle(m, isExclusiveBlock: true));

        concurrentMessageHandler.LinkTo(filterNullResponses, propCompletion);
        exclusiveMessageHandler.LinkTo(filterNullResponses, propCompletion);
        filterNullResponses.LinkTo(this.outgoingMessages, propCompletion);

        // Complete the transport streams when the dataflow networks are themselves completed.
        this.messageParser.Completion.ContinueWith(t => this.transport.Input.Complete(t.Exception), TaskContinuationOptions.ExecuteSynchronously);
        this.outgoingMessages.Completion.ContinueWith(t => this.transport.Output.Complete(t.Exception), TaskContinuationOptions.ExecuteSynchronously);
    }

    public void AddRpcMethod(MethodInfo handlerMethod, object? target)
    {
        var handler = new LanguageServerMethodHandler(handlerMethod, target);
        this.methodHandlers.Add(handler.MethodName, handler);
    }

    public Task ListenAsync()
    {
        this.messageParser.Post(this.DeserializeFromTransport());
        this.messageParser.Complete();
        return this.outgoingMessages.Completion;
    }

    private async IAsyncEnumerable<LspMessage> DeserializeFromTransport()
    {
        while (true)
        {
            LspMessage? message;

            try
            {
                // We don't pass the CancellationToken all the way down to PipeReader because it has no effect
                // on standard I/O (the I/O itself cannot be canceled).    
                message = await this.ReadLspMessage().WaitAsync(this.shutdownTokenSource.Token);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == this.shutdownTokenSource.Token)
            {
                // If Shutdown is called, we do not want the dataflow network to end in a faulted state.
                break;
            }
            catch (JsonException ex)
            {
                // If we're sent invalid JSON, we're not really sure what we're responding to.
                // According to the spec, we should send an error response with a null id.
                this.outgoingMessages.Post(CreateResponseMessage(id: null, FromJsonException(ex)));
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

    private async Task<LspMessage?> ReadLspMessage()
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

    private static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, ref int contentLength, out LspMessage message)
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
                    message = JsonSerializer.Deserialize<LspMessage>(ref jsonReader, jsonDeserializerOptions);
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

    private async Task SerializeToTransport(LspMessage message)
    {
        var writer = this.transport.Output;

        void WriteData()
        {
            var response = JsonSerializer.SerializeToUtf8Bytes(message, jsonSerializerOptions);

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

    private async Task<LspMessage?> ProcessRequestOrNotification(LspMessage message)
    {
        string method;
        JsonElement? @params;
        OneOf<int, string>? id;

        if (message.Is<RequestMessage>(out var requestMessage))
        {
            method = requestMessage.Method;
            @params = requestMessage.Params;
            id = requestMessage.Id;
        }
        else if (message.Is<NotificationMessage>(out var notificationMessage))
        {
            method = notificationMessage.Method;
            @params = notificationMessage.Params;
            id = null;
        }
        else
        {
            throw new InvalidDataException();
        }

        if (method.StartsWith("$/"))
        {
            return this.ProcessImplementationDefinedMethod(method, @params, id);
        }

        LspMessage? Error(ResponseError error)
        {
            // We can't respond to a notification, even if we hit an error.
            if (id is not null)
            {
                return CreateResponseMessage(id.Value, error);
            }

            return null;
        }

        if (!this.methodHandlers.TryGetValue(method, out var handler))
        {
            return Error(new()
            {
                Code = -32601, // MethodNotFound
                Message = $"A handler for the method '{method}' was not registered.",
            });
        }

        // If the params value exists, it must be an object or an array or the message is malformed JSON-RPC.
        if (@params is JsonElement { ValueKind: not (JsonValueKind.Object or JsonValueKind.Array) })
        {
            return Error(new()
            {
                Code = -32600, // InvalidRequest
                Message = "Invalid request.",
            });
        }

        if (handler.ProducesResponse && id is null)
        {
            // The client sent a notification, but we have a request handler for this method.
            return null;
        }
        else if (!handler.ProducesResponse && id is not null)
        {
            // The client sent a request, but we have a notification handler for this method.
            return Error(new()
            {
                Code = -32603, // InternalError
                Message = $"A handler for the method '{method}' was registered as a notification handler.",
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
                    args.Add(@params.Value.Deserialize(handler.DeclaredParamsType, jsonDeserializerOptions));
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
                // Only requests can be canceled, so we don't need to create a CancellationTokenSource for a notification.
                cts = new CancellationTokenSource();
                this.pendingIncomingRequests.TryAdd(id!.Value, cts);
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
            this.pendingIncomingRequests.Remove(id!.Value, out _);
            cts.Dispose();
        }

        if (handler.ProducesResponse)
        {
            OneOf<object?, ResponseError> serializableResponse;
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

                serializableResponse = new ResponseError
                {
                    Message = error?.Message ?? "Internal error while processing request.",
                    Code = errorCode,
                    Data = JsonSerializer.SerializeToElement(error?.ToString()),
                };
            }
            else
            {
                serializableResponse = result.As<object?>();
            }

            return CreateResponseMessage(id!.Value, serializableResponse);
        }

        // No response
        return null;
    }

    private static ResponseMessage CreateResponseMessage(OneOf<int, string>? id, OneOf<object?, ResponseError> response)
    {
        var responseMessage = new ResponseMessage
        {
            Jsonrpc = "2.0",
            Id = id!,
        };

        if (response.Is<ResponseError>(out var error))
        {
            responseMessage.Error = error;
        }
        else
        {
            responseMessage.Result = JsonSerializer.SerializeToElement(response.As<object>(), jsonSerializerOptions);
        }

        return responseMessage;
    }

    private static ResponseError FromJsonException(JsonException ex)
    {
        // Typically, only exceptions from Utf8JsonReader have the position info set.
        // So, we assume this is a parse error if it's there, and other errors are serialization errors.
        var code = ex.BytePositionInLine.HasValue
            ? -32700  // ParseError
            : -32602; // InvalidParams

        var responseError = new ResponseError
        {
            Message = ex.Message,
            Data = JsonSerializer.SerializeToElement(ex.ToString()),
            Code = code,
        };

        return responseError;
    }

    private LspMessage? ProcessImplementationDefinedMethod(string method, JsonElement? @params, OneOf<int, string>? id)
    {
        if (id is not null)
        {
            if (method == "$/cancelRequest")
            {
                if (this.pendingIncomingRequests.TryGetValue(id.Value, out var cts))
                {
                    cts.Cancel();
                }
            }
        }
        else
        {
            // If we supported progress notifications, we would handle them here.
        }

        return null;
    }

    public void PostNotification(string method, object? @params)
    {
        var serializedParams = JsonSerializer.SerializeToElement(@params, jsonSerializerOptions);
        this.outgoingMessages.Post(new NotificationMessage
        {
            Jsonrpc = "2.0",
            Method = method,
            Params = serializedParams,
        });
    }

    public async Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params)
    {
        var id = Interlocked.Increment(ref this.lastMessageId);

        var serializedParams = JsonSerializer.SerializeToElement(@params, jsonSerializerOptions);
        this.outgoingMessages.Post(new RequestMessage
        {
            Jsonrpc = "2.0",
            Method = method,
            Id = id,
            Params = serializedParams,
        });

        JsonElement response;
        var block = new WriteOnceBlock<LspMessage>(null);

        // Response messages are not handled by the dataflow blocks built in LanguageServerConnection's constructor. Instead, when we send a request to the client,
        // we create a new block that will only consume a response message with the same ID we used for the request. The block is removed from the network after
        // the response is received.
        bool IsThisResponse(LspMessage m) => m.Is<ResponseMessage>(out var r) && r.Id?.As<int>() == id;
        using (this.messageParser.LinkTo(block, new() { MaxMessages = 1 }, IsThisResponse))
        {
            var responseMessage = (await block.ReceiveAsync()).As<ResponseMessage>();

            if (this.pendingIncomingRequests.Remove(id, out var cts))
            {
                cts.Dispose();
            }

            if (responseMessage.Error is { } error)
            {
                throw new LspResponseException(error);
            }

            response = responseMessage.Result;
        };

        return response.Deserialize<TResponse>(jsonDeserializerOptions);
    }

    public void Shutdown() => this.shutdownTokenSource.Cancel();
}

internal sealed class LspResponseException : Exception
{
    public LspResponseException(ResponseError error) : base(error.Message)
    {
        this.ResponseError = error;
    }

    public ResponseError ResponseError { get; }
}
