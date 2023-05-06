using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

using LspMessage = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RequestMessage, Draco.Lsp.Model.NotificationMessage, Draco.Lsp.Model.ResponseMessage>;

namespace Draco.Lsp.Protocol;

public sealed class LspConnection
{
    private readonly IDuplexPipe transport;
    private readonly Dictionary<string, RpcMethodHandler> methodHandlers = new();

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
        }
    };

    public LspConnection(IDuplexPipe pipeTransport)
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

        this.outgoingMessages = new ActionBlock<LspMessage>(this.SerializeObject!);

        bool IsRequestOrNotification(LspMessage message, bool mutating)
        {
            if (message.Is<RequestMessage>(out var request))
            {
                return this.methodHandlers[request.Method].IsMutating == mutating;
            }
            else if (message.Is<NotificationMessage>(out var notification))
            {
                return this.methodHandlers[notification.Method].IsMutating == mutating;
            }
            else
            {
                return false;
            }
        }

        var propCompletion = new DataflowLinkOptions { PropagateCompletion = true };

        this.messageParser.LinkTo(concurrentMessageHandler, propCompletion, m => IsRequestOrNotification(m, mutating: false));
        this.messageParser.LinkTo(exclusiveMessageHandler, propCompletion, m => IsRequestOrNotification(m, mutating: true));

        concurrentMessageHandler.LinkTo(filterNullResponses, propCompletion);
        exclusiveMessageHandler.LinkTo(filterNullResponses, propCompletion);
        filterNullResponses.LinkTo(this.outgoingMessages, propCompletion);

        this.messageParser.Completion.ContinueWith(t => this.transport.Input.Complete(t.Exception), TaskContinuationOptions.ExecuteSynchronously);
        this.outgoingMessages.Completion.ContinueWith(t => this.transport.Output.Complete(t.Exception), TaskContinuationOptions.ExecuteSynchronously);
    }

    public LspConnection(Stream streamTransport) : this(new StreamDuplexPipe(streamTransport))
    {
    }

    private sealed class StreamDuplexPipe : IDuplexPipe
    {
        public StreamDuplexPipe(Stream transport)
        {
            this.Input = PipeReader.Create(transport);
            this.Output = PipeWriter.Create(transport);
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
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

    public void AddRpcMethod(RpcMethodHandler handler)
    {
        var @params = handler.MethodInfo.GetParameters();

        if (@params.Length > 2)
        {
            throw new ArgumentException("Handling method has too many parameters.", nameof(handler));
        }

        var cancellationCount = @params.Count(p => p.ParameterType == typeof(CancellationToken));
        if (cancellationCount > 1)
        {
            throw new ArgumentException("Handling method can have at most one CancellationToken parameter.", nameof(handler));
        }

        var hasCancellation = cancellationCount > 0;
        if (@params.Length > 1 && !hasCancellation)
        {
            throw new ArgumentException("Handling method can have at most one non-CancellationToken parameter.", nameof(handler));
        }

        if (!handler.IsRequestHandler && hasCancellation)
        {
            // A handler cannot be cancelled.
            // TODO-LSP: Should we do something?
        }

        this.methodHandlers.Add(handler.MethodName, handler);
    }

    public Task ListenAsync()
    {
        System.Diagnostics.Debugger.Launch();
        this.messageParser.Post(this.ReadLspMessages());
        this.messageParser.Complete();
        return this.outgoingMessages.Completion;
    }

    private async IAsyncEnumerable<LspMessage> ReadLspMessages()
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
                    // TODO-LSP: We should do something in response.
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
                    // TODO-LSP: Handle failure
                }
            }
            else if (headerText.StartsWith(ContentTypeHeader))
            {
                // TODO-LSP: In theory, we are supposed to ensure that the requested encoding is UTF-8.
            }
        }

        message = default;
        return false;
    }

    private async Task SerializeObject(LspMessage message)
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
            // ?
        }
    }

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
            throw new UnreachableException();
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

        if (!this.methodHandlers.TryGetValue(method, out var methodHandler))
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

        var (_, methodInfo, target, isRequest, _) = methodHandler;

        if (isRequest && id == null)
        {
            // The client sent a message without an id, but the method was marked as a request
            // in the managed metadata. Something isn't right.
            return null;
        }
        else if (id != null && !isRequest)
        {
            // The client sent a message with an id, but the method was marked as a notification
            // in the managed metadata. We need to respond with something.
            return Error(new()
            {
                Code = -32603, // InternalError
                Message = $"A handler for the method '{method}' was registered as a notification handler."
            });
        }

        var args = new List<object?>();

        if (@params.HasValue)
        {
            try
            {
                var element = @params.Value;
                args.Add(JsonSerializer.Deserialize(element, methodHandler.ParameterType!, jsonOptions));
            }
            catch (JsonException ex)
            {
                var responseError = new ResponseError
                {
                    Message = ex.Message,
                    Data = JsonSerializer.SerializeToElement(ex.ToString())
                };

                if (ex.BytePositionInLine.HasValue)
                {
                    // Typically, only exceptions from Utf8JsonReader have the position info set.
                    // So, we assume this is a parse error, and other errors are serialization errors.
                    responseError.Code = -32700; // ParseError
                }
                else
                {
                    responseError.Code = -32602; // InvalidParams
                }

                return Error(responseError);
            }
        }

        var cancellationToken = CancellationToken.None;

        if (isRequest)
        {
            var cts = new CancellationTokenSource();
            this.pendingIncomingRequests.TryAdd(id!.Value, cts);
            cancellationToken = cts.Token;
        }

        var index = methodHandler.CancellationTokenParameterIndex;
        if (index != -1)
        {
            args.Insert(index, cancellationToken);
        }

        OneOf<object?, Exception> response;
        try
        {
            var returnValue = methodInfo.Invoke(target, args.ToArray());
            response = await GetReturnValueToSerialize(returnValue, methodInfo.ReturnType);
        }
        catch (Exception ex)
        {
            response = ex;
        }

        if (id != null)
        {
            OneOf<object?, ResponseError> serializableResponse;
            if (response.Is<Exception>(out var error))
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

                serializableResponse = new ResponseError
                {
                    Message = error?.Message ?? "",
                    Code = errorCode,
                    Data = JsonSerializer.SerializeToElement(error?.ToString())
                };
            }
            else
            {
                serializableResponse = response.As<object?>();
            }

            return CreateResponseMessage(id.Value, serializableResponse);
        }

        return null;
    }

    private static ResponseMessage CreateResponseMessage(OneOf<int, string> id, OneOf<object?, ResponseError> response)
    {
        var responseMessage = new ResponseMessage
        {
            Jsonrpc = "2.0",
            Id = id
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

    private static readonly MethodInfo taskGetResult = typeof(Task<>).GetMethod("get_Result")!;

    private static async Task<object?> GetReturnValueToSerialize(object? returnValue, Type declaredReturnType)
    {
        var result = returnValue;

        if (returnValue is Task task)
        {
            await task;

            if (declaredReturnType == typeof(Task))
            {
                result = null;
            }
            else if (declaredReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var getResult = (MethodInfo)task.GetType().GetMemberWithSameMetadataDefinitionAs(taskGetResult);
                result = getResult.Invoke(task, null);
            }
        }

        return result;
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

public sealed record RpcMethodHandler(string MethodName, MethodInfo MethodInfo, object Target, bool IsRequestHandler, bool IsMutating)
{
    internal Type? ParameterType { get; } = MethodInfo.GetParameters().FirstOrDefault()?.ParameterType;

    internal int CancellationTokenParameterIndex { get; } = Array.FindIndex(MethodInfo.GetParameters(), a => a.ParameterType == typeof(CancellationToken));
}

public sealed class LspResponseException : Exception
{
    public LspResponseException(ResponseError error) : base(error.Message)
    {
        this.ResponseError = error;
    }

    public ResponseError ResponseError { get; }
}
