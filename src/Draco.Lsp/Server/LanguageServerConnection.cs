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
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Draco.Lsp.Model;
using Draco.Lsp.Serialization;

using LspMessage = Draco.Lsp.Model.OneOf<Draco.Lsp.Serialization.RequestMessage, Draco.Lsp.Serialization.NotificationMessage, Draco.Lsp.Serialization.ResponseMessage>;

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

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { AddResponseMessageContract }
        },
        Converters = { new TupleConverter() }
    };

    public LanguageServerConnection(IDuplexPipe pipeTransport)
    {
        this.transport = pipeTransport;

        var scheduler = new ConcurrentExclusiveSchedulerPair();

        this.messageParser = new TransformManyBlock<IAsyncEnumerable<LspMessage>, LspMessage>(
            ae => ae,
            new()
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            });

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
            });

        var filterNullResponses = new TransformManyBlock<LspMessage?, LspMessage>(
            m => m.HasValue ? new[] { m.Value } : Enumerable.Empty<LspMessage>());

        this.outgoingMessages = new ActionBlock<LspMessage>(this.SerializeToTransport!);

        bool ShouldHandle(LspMessage message, bool mutating)
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
            else
            {
                return false;
            }

            if (this.methodHandlers.TryGetValue(method, out var handler))
            {
                return mutating == handler.Mutating;
            }

            // Handle in the concurrent block.
            return mutating == false;
        }

        var propCompletion = new DataflowLinkOptions { PropagateCompletion = true };

        this.messageParser.LinkTo(concurrentMessageHandler, propCompletion, m => ShouldHandle(m, mutating: false));
        this.messageParser.LinkTo(exclusiveMessageHandler, propCompletion, m => ShouldHandle(m, mutating: true));

        concurrentMessageHandler.LinkTo(filterNullResponses, propCompletion);
        exclusiveMessageHandler.LinkTo(filterNullResponses, propCompletion);
        filterNullResponses.LinkTo(this.outgoingMessages, propCompletion);

        this.messageParser.Completion.ContinueWith(t => this.transport.Input.Complete(t.Exception), TaskContinuationOptions.ExecuteSynchronously);
        this.outgoingMessages.Completion.ContinueWith(t => this.transport.Output.Complete(t.Exception), TaskContinuationOptions.ExecuteSynchronously);
    }

    private static void AddResponseMessageContract(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(ResponseMessage))
        {
            var resultProperty = typeInfo.Properties.Single(p => p.Name == "result");

            resultProperty.ShouldSerialize = (responseObject, _) =>
            {
                var response = (ResponseMessage)responseObject;
                // The result property MUST only appear on the object if the request was successful
                // (in which case, the error property MUST be null).
                return response.Error == null;
            };
        }
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

            if (message != null)
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

            var foundJson = this.TryParseMessage(ref buffer, ref contentLength, out var message);

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

    private bool TryParseMessage(ref ReadOnlySequence<byte> buffer, ref int contentLength, out LspMessage message)
    {
        var reader = new SequenceReader<byte>(buffer);

        while (reader.TryReadTo(sequence: out var header, "\r\n"u8))
        {
            if (header.IsEmpty)
            {
                // We've just scanned past the final header, and we're now ready to
                // read the JSON payload.
                if (reader.TryReadExact(contentLength, out var utf8Json))
                {
                    buffer = buffer.Slice(reader.Position, reader.Position);

                    var jsonReader = new Utf8JsonReader(utf8Json);
                    message = JsonSerializer.Deserialize<LspMessage>(ref jsonReader, jsonOptions);
                    return true;
                }
                else
                {
                    // Keep the \r\n right before the JSON payload.
                    // Otherwise, we'll never enter this loop again.
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
            var response = JsonSerializer.SerializeToUtf8Bytes(message, jsonOptions);

            var ContentLengthHeader = "Content-Length: "u8;
            var TwoNewLines = "\r\n\r\n"u8;

            var written = 0;
            var responseBuffer = writer.GetSpan(response.Length + ContentLengthHeader.Length + 16);

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
            if (id != null)
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
                Message = $"A handler for the method '{method}' was not registered."
            });
        }

        if (@params is JsonElement { ValueKind: not (JsonValueKind.Object or JsonValueKind.Array) })
        {
            return Error(new()
            {
                Code = -32600, // InvalidRequest
                Message = "Invalid request."
            });
        }

        if (handler.ProducesResponse && id == null)
        {
            // The client sent a notification, but we have a request handler for this method.
            return null;
        }
        else if (!handler.ProducesResponse && id != null)
        {
            // The client sent a request, but we have a notification handler for this method.
            return Error(new()
            {
                Code = -32603, // InternalError
                Message = $"A handler for the method '{method}' was registered as a notification handler."
            });
        }

        var args = new List<object?>();
        if (handler.DeclaredParamsType != null)
        {
            if (@params.HasValue)
            {
                try
                {
                    args.Add(@params.Value.Deserialize(handler.DeclaredParamsType!, jsonOptions));
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

        if (handler.HasCancellation)
        {
            var token = CancellationToken.None;

            if (id != null)
            {
                var cts = new CancellationTokenSource();
                this.pendingIncomingRequests.TryAdd(id.Value, cts);
                token = cts.Token;
            }

            args.Add(token);

        }

        OneOf<object?, Exception> result;
        try
        {
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

        if (handler.ProducesResponse)
        {
            OneOf<object?, ResponseError> response;
            if (result.Is<Exception>(out var error))
            {
                if (error is TargetInvocationException)
                {
                    error = error.InnerException;
                }

                var errorCode = error switch
                {
                    OperationCanceledException => -32800, // RequestCancelled
                    _ => -32603 // InternalError
                };

                response = new ResponseError
                {
                    Message = error?.Message ?? "",
                    Code = errorCode,
                    Data = JsonSerializer.SerializeToElement(error?.ToString())
                };
            }
            else
            {
                response = result.As<object?>();
            }

            return CreateResponseMessage(id!.Value, response);
        }

        return null;
    }

    private static ResponseMessage CreateResponseMessage(OneOf<int, string>? id, OneOf<object?, ResponseError> response)
    {
        var responseMessage = new ResponseMessage
        {
            Jsonrpc = "2.0",
            Id = id!
        };

        if (response.Is<ResponseError>(out var error))
        {
            responseMessage.Error = error;
        }
        else
        {
            responseMessage.Result = JsonSerializer.SerializeToElement(response.As<object>(), jsonOptions);
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
            Code = code
        };

        return responseError;
    }

    private LspMessage? ProcessImplementationDefinedMethod(string method, JsonElement? @params, OneOf<int, string>? id)
    {
        if (id != null)
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
        var serializedParams = JsonSerializer.SerializeToElement(@params, jsonOptions);
        this.outgoingMessages.Post(new NotificationMessage
        {
            Jsonrpc = "2.0",
            Method = method,
            Params = serializedParams
        });
    }

    public async Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params)
    {
        var id = Interlocked.Increment(ref this.lastMessageId);

        var serializedParams = JsonSerializer.SerializeToElement(@params, jsonOptions);
        this.outgoingMessages.Post(new RequestMessage
        {
            Jsonrpc = "2.0",
            Method = method,
            Id = id,
            Params = serializedParams
        });

        JsonElement response;
        var block = new WriteOnceBlock<LspMessage>(null);

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

            response = responseMessage.Result!.GetValueOrDefault();
        };

        if (response.ValueKind == JsonValueKind.Undefined)
        {
            return default;
        }
        else
        {
            return response.Deserialize<TResponse>(jsonOptions);
        }
    }

    public void Shutdown()
    {
        this.shutdownTokenSource.Cancel();
    }
}

internal sealed class LspResponseException : Exception
{
    public LspResponseException(ResponseError error) : base(error.Message)
    {
        this.ResponseError = error;
    }

    public ResponseError ResponseError { get; }
}
