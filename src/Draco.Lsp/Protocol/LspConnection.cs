using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

using Draco.Lsp.Model;

using LspMessage = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RequestMessage, Draco.Lsp.Model.NotificationMessage, Draco.Lsp.Model.ResponseMessage>;

namespace Draco.Lsp.Protocol;

public sealed class LspConnection : IAsyncDisposable
{
    private readonly IDuplexPipe transport;
    private readonly JsonSerializerOptions options;
    private readonly Dictionary<string, JsonRpcMethodHandler> methodHandlers = new();

    // These channels are bounded to prevent infinite memory growth in case of a bug in the language server.
    private readonly Channel<LspMessage> outgoingMessages = Channel.CreateBounded<LspMessage>(new BoundedChannelOptions(128));
    private readonly Channel<LspMessage> incomingMessages = Channel.CreateBounded<LspMessage>(new BoundedChannelOptions(128));


    private int lastMessageId = 0;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> inFlightRequests = new();

    public LspConnection(IDuplexPipe pipeTransport)
    {
        this.transport = pipeTransport;
        // TODO-LSP: Do we want to take the options as a parameter?
        this.options = new JsonSerializerOptions();

        this.options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { this.AddResponseMessageContract }
        };
    }

    private void AddResponseMessageContract(JsonTypeInfo typeInfo)
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

    public LspConnection(Stream streamTransport) : this(new StreamDuplexPipe(streamTransport))
    {
    }

    private sealed class StreamDuplexPipe : IDuplexPipe, IAsyncDisposable
    {
        private readonly Stream transport;

        public StreamDuplexPipe(Stream transport)
        {
            this.transport = transport;
            this.Input = PipeReader.Create(transport);
            this.Output = PipeWriter.Create(transport);
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public ValueTask DisposeAsync() => this.transport.DisposeAsync();
    }

    public void AddRpcMethod(JsonRpcMethodHandler handler)
    {
        var @params = handler.MethodInfo.GetParameters();
        if (@params is [.., { ParameterType: Type t }] && t == typeof(CancellationToken))
        {
            @params = @params[..^1];
        }

        // TODO-LSP: We need to actually flow the cancellation token throughout this entire system.
        // We haven't done that yet.
        // TODO-LSP: It also needs to be integrated with the LSP cancellation request.

        if (@params.Length > 1)
        {
            throw new ArgumentException("Handling method must have at most one parameter.", nameof(handler));
        }

        this.methodHandlers.Add(handler.MethodName, handler);
    }

    public Task ListenAsync()
    {
        var reader = this.ReadIncomingMessages();
        var writer = this.WriteOutgoingMessages();

        var incoming = this.ProcessIncomingMessages();

        return Task.WhenAny(Task.WhenAll(reader, writer), incoming);
    }

    private async Task<LspMessage> ReadLspMessage()
    {
        var contentLength = -1;
        var reader = this.transport.Input;

        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            var consumed = buffer.Start;
            var examined = buffer.End;

            bool ExamineBuffer(out LspMessage message)
            {
                var reader = new SequenceReader<byte>(buffer);

                while (reader.TryReadTo(sequence: out var header, "\r\n"u8))
                {
                    consumed = reader.Position;

                    if (header.IsEmpty)
                    {
                        // We've just scanned past the final header, and we're now ready to
                        // read the JSON payload.
                        if (reader.TryReadExact(contentLength, out var jsonBuf))
                        {
                            consumed = reader.Position;
                            examined = consumed;

                            // We might want to build up the Utf8JsonReader as we scan through the JSON,
                            // instead of all at once.
                            var jsonReader = new Utf8JsonReader(jsonBuf);
                            message = JsonSerializer.Deserialize<LspMessage>(ref jsonReader, this.options);
                            return true;
                        }
                        else
                        {
                            // Make sure we keep the \r\n right before the JSON payload.
                            // Otherwise, this loop will immediately exit the next time we return.
                            reader.Rewind(2);
                            consumed = reader.Position;
                            break;
                        }
                    }

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

            var foundJson = ExamineBuffer(out var message);

            if (result.IsCompleted)
            {
                if (buffer.Length > 0)
                {
                    // The stream abruptly ended.
                    // TODO-LSP: We should do something in response.
                }

                break;
            }

            reader.AdvanceTo(consumed, examined);

            if (foundJson)
            {
                return message;
            }
        }

        return default;
    }

    private async Task ReadIncomingMessages()
    {
        try
        {
            while (true)
            {
                var message = await this.ReadLspMessage();
                Debug.WriteLine($"INCOMING: {JsonSerializer.Serialize(((IOneOf)message).Value)}");

                await this.incomingMessages.Writer.WriteAsync(message);
            }
        }
        catch (Exception ex)
        {
            this.transport.Input.Complete(ex);
            this.incomingMessages.Writer.Complete(ex);
            // TODO-LSP: Is this the right place to do this?
            this.outgoingMessages.Writer.Complete(ex);
        }
    }


    private async Task WriteOutgoingMessages()
    {
        try
        {
            using var stream = this.transport.Output.AsStream();

            var ContentLengthHeader = "Content-Length: "u8.ToArray();
            var TwoNewLines = "\r\n\r\n"u8.ToArray();

            await foreach (var message in this.outgoingMessages.Reader.ReadAllAsync())
            {
                Debug.WriteLine($"OUTGOING: {JsonSerializer.Serialize(((IOneOf)message).Value)}");
                var response = JsonSerializer.SerializeToUtf8Bytes(message, this.options);
                await stream.WriteAsync(ContentLengthHeader);

                var contentLength = ArrayPool<byte>.Shared.Rent(128);
                Utf8Formatter.TryFormat(response.Length, contentLength, out var contentLengthLength);
                await stream.WriteAsync(contentLength.AsMemory(0, contentLengthLength));

                await stream.WriteAsync(TwoNewLines);
                await stream.WriteAsync(response);
            }
        }
        catch (Exception ex)
        {
            // TODO-LSP: Is this the right place for this?
            // TODO-LSP: All of the exception handling in this file needs to be cleaned up
            this.transport.Output.Complete(ex);
        }
    }

    private async Task ProcessIncomingMessages()
    {
        try
        {
            // TODO-LSP: If this method throws, the exception is hidden by WhenAll
            // and the server hangs forever. We need to figure out what we should do in response
            // to an exception in this method.
            await foreach (var message in this.incomingMessages.Reader.ReadAllAsync())
            {
                // TODO-LSP: The lack of awaits here means that request processing can happen in parallel.
                // Is this what we want? Do we want some way to configure this?
                // Using await here causes deadlocks (if processing a request invokes another request and
                // waits for its response).
                if (message.Is<RequestMessage>(out var requestMessage))
                {
                    _ = this.ProcessRequestOrNotification(requestMessage.Method, requestMessage.Params, requestMessage.Id);
                }
                else if (message.Is<NotificationMessage>(out var notificationMessage))
                {
                    _ = this.ProcessRequestOrNotification(notificationMessage.Method, notificationMessage.Params, null);
                }
                else if (message.Is<ResponseMessage>(out var responseMessage))
                {
                    this.ProcessResponseMessage(responseMessage);
                }
                else
                {
                    throw new InvalidDataException();
                }
            }
        }
        catch (Exception ex)
        {
            ;
        }
    }

    private static readonly MethodInfo taskGetResult = typeof(Task<>).GetMethod("get_Result")!;

    private async Task ProcessRequestOrNotification(string method, JsonElement? @params, OneOf<int, string>? id)
    {
        var methodHandler = this.methodHandlers[method];
        // TODO-LSP: Do we need IsRequest for anything?
        var (_, methodInfo, target, isRequest) = methodHandler;

        var index = methodHandler.CancellationTokenParameterIndex;
        var builder = ImmutableArray.CreateBuilder<object?>();

        if (@params.HasValue)
        {
            var element = @params.Value;
            builder.Add(JsonSerializer.Deserialize(element, methodHandler.ParameterType!, this.options));
        }

        if (index != -1)
        {
            builder.Insert(index, CancellationToken.None);
        }


        object? result = null;
        Exception? error = null;
        try
        {
            var returnValue = methodInfo.Invoke(target, builder.ToArray());

            // TODO-LSP We might want to support more types of return types here.
            if (returnValue is Task task)
            {
                await task;

                if (task.GetType().GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var getResult = (MethodInfo)task.GetType().GetMemberWithSameMetadataDefinitionAs(taskGetResult);
                    result = getResult.Invoke(task, null);
                }
            }
            else
            {
                result = returnValue;
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }

        if (id != null)
        {
            var response = new ResponseMessage
            {
                Jsonrpc = "2.0",
                Id = id
            };

            if (error is not null)
            {
                response.Error = new ResponseError()
                {
                    Message = error is TargetInvocationException ? error.InnerException!.Message : error.Message,
                    // TODO-RPC: This obviously needs to be a different
                    Code = error.HResult,
                    Data = JsonSerializer.SerializeToElement(error.ToString())
                };
            }
            else
            {
                response.Result = JsonSerializer.SerializeToElement(result, this.options);
            }

            await this.outgoingMessages.Writer.WriteAsync(response);
        }
    }

    private void ProcessResponseMessage(ResponseMessage responseMessage)
    {
        var id = responseMessage.Id!.Value.As<int>();
        if (this.inFlightRequests.TryRemove(id, out var tcs))
        {
            if (responseMessage.Error is not null)
            {
                var error = responseMessage.Error;
                tcs.TrySetException(new LspResponseException(error!));
            }
            else
            {
                tcs.TrySetResult(responseMessage.Result.GetValueOrDefault());
            }
        }
    }

    public async Task InvokeNotification(string method, object? @params)
    {
        var serializedParams = JsonSerializer.SerializeToElement(@params, this.options);
        await this.outgoingMessages.Writer.WriteAsync(new NotificationMessage
        {
            Jsonrpc = "2.0",
            Method = method,
            Params = serializedParams
        });
    }

    public async Task<TResponse?> InvokeRequest<TResponse>(string method, object? @params)
    {
        var id = Interlocked.Increment(ref this.lastMessageId);
        var tcs = new TaskCompletionSource<JsonElement>();
        this.inFlightRequests.TryAdd(id, tcs);

        // TODO-LSP: This is probably not as efficient as it could be
        var serializedParams = JsonSerializer.SerializeToElement(@params, this.options);
        await this.outgoingMessages.Writer.WriteAsync(new RequestMessage
        {
            Jsonrpc = "2.0",
            Method = method,
            Id = id,
            Params = serializedParams
        });

        return (await tcs.Task).Deserialize<TResponse>(this.options);
    }

    public ValueTask DisposeAsync()
    {
        // TODO-LSP: We want to post "poison" messages to our queues that will tell them to shut down,
        // and then wait for the pipes to complete from here.
        return default;
    }
}

public sealed record JsonRpcMethodHandler(string MethodName, MethodInfo MethodInfo, object Target, bool IsRequestHandler)
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
