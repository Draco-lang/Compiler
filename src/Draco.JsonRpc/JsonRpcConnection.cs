using System.Buffers.Text;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Channels;
using System.Collections.Concurrent;

namespace Draco.JsonRpc;

/// <summary>
/// A base class for a JSON-RPC connection.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TError">The error descriptor.</typeparam>
/// <typeparam name="TMessageAdapter">The message adapter to query info about the messages.</typeparam>
public abstract class JsonRpcConnection<TMessage, TError, TMessageAdapter> : IJsonRpcConnection
    where TMessageAdapter : IJsonRpcMessageAdapter<TMessage, TError>
{
    private interface IOutgoingRequest
    {
        public Task Task { get; }
        public Type ResponseType { get; }

        public void Cancel();
        public void Complete(object? result);
    }

    private sealed class OutgoingRequest<TResponse> : IOutgoingRequest
    {
        public Task Task => this.tcs.Task;
        public Type ResponseType => typeof(TResponse);

        private readonly TaskCompletionSource<TResponse?> tcs = new();

        public void Cancel() => this.tcs.SetCanceled();
        public void Complete(object? result) => this.tcs.SetResult((TResponse?)result);
    }

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

    // Handlers
    private readonly Dictionary<string, IJsonRpcMethodHandler> methodHandlers = new();

    // Pending requests
    private readonly ConcurrentDictionary<object, CancellationTokenSource> pendingIncomingRequests = new();
    private readonly ConcurrentDictionary<int, IOutgoingRequest> pendingOutgoingRequests = new();

    // Communication state
    private int lastMessageId = 0;

    public void AddHandler(IJsonRpcMethodHandler handler) => this.methodHandlers.Add(handler.MethodName, handler);

    public Task ListenAsync() => Task.WhenAll(
        this.ReaderLoopAsync(),
        this.WriterLoopAsync(),
        this.ProcessorLoopAsync());

    protected int NextMessageId() => Interlocked.Increment(ref this.lastMessageId);

    #region Message Loops
    private async Task ReaderLoopAsync()
    {
        while (true)
        {
            var (message, foundMessage) = await this.ReadMessageAsync();
            if (!foundMessage) break;

            if (TMessageAdapter.IsResponse(message!))
            {
                this.ProcessIncomingResponse(message!);
            }
            else
            {
                await this.incomingMessages.Writer.WriteAsync(message!);
            }
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
        bool IsMutating(TMessage message)
        {
            var method = TMessageAdapter.GetMethodName(message);
            return this.methodHandlers.TryGetValue(method, out var handler)
                && handler.Mutating;
        }

        var currentTasks = new List<Task>();
        await foreach (var message in this.incomingMessages.Reader.ReadAllAsync())
        {
            if (IsMutating(message))
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

    #region Message Processing
    protected async Task ProcessMessageAsync(TMessage message)
    {
        await Task.Yield();

        if (TMessageAdapter.IsRequest(message))
        {
            var response = await this.ProcessIncomingRequestAsync(message);
            await this.outgoingMessages.Writer.WriteAsync(response);
        }
        else if (TMessageAdapter.IsNotification(message))
        {
            await this.ProcessIncomingNotificationAsync(message);
        }
        else
        {
            // TODO: What to do?
        }
    }

    private async Task<TMessage> ProcessIncomingRequestAsync(TMessage message)
    {
        var messageId = TMessageAdapter.GetId(message);
        var methodName = TMessageAdapter.GetMethodName(message);
        var @params = TMessageAdapter.GetParams(message);

        TMessage Error(TError error) => TMessageAdapter.CreateErrorResponse(messageId!, error);

        // Cancellation handling
        if (TMessageAdapter.IsCancellation(message))
        {
            this.CancelIncomingRequest(messageId!);
            return TMessageAdapter.CreateOkResponse(messageId!, default);
        }

        // Error handling block
        if (!this.methodHandlers.TryGetValue(methodName, out var handler))
        {
            return Error(TMessageAdapter.CreateHandlerNotRegisteredError(methodName));
        }
        if (@params is JsonElement { ValueKind: not (JsonValueKind.Object or JsonValueKind.Array) })
        {
            return Error(TMessageAdapter.CreateInvalidRequestError());
        }
        if (!handler.IsRequest)
        {
            return Error(TMessageAdapter.CreateHandlerWasRegisteredAsNotificationHandlerError(methodName));
        }

        // Build up arguments
        var args = new List<object?>();
        if (handler.AcceptsParams)
        {
            try
            {
                var arg = @params?.Deserialize(handler.DeclaredParamsType, this.JsonDeserializerOptions);
                args.Add(arg);
            }
            catch (JsonException ex)
            {
                return Error(TMessageAdapter.CreateJsonExceptionError(ex));
            }
        }
        if (handler.SupportsCancellation)
        {
            var ct = this.AddIncomingRequest(messageId!);
            args.Add(ct);
        }

        // Actually invoke handler
        var result = null as object;
        var error = null as Exception;
        try
        {
            result = await handler.InvokeRequest(args.ToArray());
        }
        catch (Exception ex)
        {
            error = ex;
        }

        // Operation completed or canceled, try completing it
        if (handler.SupportsCancellation) this.CompleteIncomingRequest(messageId!);

        // Respond appropriately
        if (error is not null)
        {
            return Error(TMessageAdapter.CreateExceptionError(error));
        }
        else
        {
            var resultJson = JsonSerializer.SerializeToElement(result, this.JsonSerializerOptions);
            return TMessageAdapter.CreateOkResponse(messageId!, resultJson);
        }
    }

    private void ProcessIncomingResponse(TMessage message)
    {
        var messageId = TMessageAdapter.GetId(message);
        var @params = TMessageAdapter.GetParams(message);

        if (@params is JsonElement { ValueKind: not (JsonValueKind.Object or JsonValueKind.Array) })
        {
            return;
        }

        this.CompleteOutgoingRequest((int)messageId!, @params);
    }

    private async Task ProcessIncomingNotificationAsync(TMessage message)
    {
        var messageId = TMessageAdapter.GetId(message);
        var methodName = TMessageAdapter.GetMethodName(message);
        var @params = TMessageAdapter.GetParams(message);

        // Cancellation handling
        if (TMessageAdapter.IsCancellation(message))
        {
            this.CancelIncomingRequest(messageId!);
            return;
        }

        // Error handling block
        // Note that we can't respond to notifications
        if (!this.methodHandlers.TryGetValue(methodName, out var handler))
        {
            return;
        }
        if (@params is JsonElement { ValueKind: not (JsonValueKind.Object or JsonValueKind.Array) })
        {
            return;
        }
        if (!handler.IsNotification)
        {
            return;
        }

        // Build up arguments
        var args = new List<object?>();
        if (handler.AcceptsParams)
        {
            try
            {
                var arg = @params?.Deserialize(handler.DeclaredParamsType, this.JsonDeserializerOptions);
                args.Add(arg);
            }
            catch (JsonException)
            {
                return;
            }
        }

        // Actually invoke handler
        try
        {
            await handler.InvokeNotification(args.ToArray());
        }
        catch
        {
        }
    }
    #endregion

    #region Sending Message
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params) =>
        this.SendRequestAsync<TResponse>(method, @params, CancellationToken.None);
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params, CancellationToken cancellationToken)
    {
        // Construct request
        var id = this.NextMessageId();
        var serializedParams = JsonSerializer.SerializeToElement(@params, this.JsonSerializerOptions);
        var request = TMessageAdapter.CreateRequest(id, method, serializedParams);

        // Add the request to pending
        var pendingReq = this.AddOutgoingRequest<TResponse>(id);

        // When canceled, cancel corresponding TCS and write cancel message
        cancellationToken.Register(() => this.CancelOutgoingRequest(id));

        // Actually send message
        this.outgoingMessages.Writer.TryWrite(request);
        return (Task<TResponse?>)pendingReq.Task;
    }

    public async Task SendNotificationAsync(string method, object? @params)
    {
        var serializedParams = JsonSerializer.SerializeToElement(@params, this.JsonSerializerOptions);
        var notification = TMessageAdapter.CreateNotification(method, serializedParams);
        await this.outgoingMessages.Writer.WriteAsync(notification);
    }
    #endregion

    #region Request Response
    private CancellationToken AddIncomingRequest(object id)
    {
        var cts = new CancellationTokenSource();
        this.pendingIncomingRequests.TryAdd(id, cts);
        return cts.Token;
    }

    private void CancelIncomingRequest(object id)
    {
        if (this.pendingIncomingRequests.TryRemove(id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private void CompleteIncomingRequest(object id)
    {
        if (this.pendingIncomingRequests.TryRemove(id, out var cts)) cts.Dispose();
    }

    private IOutgoingRequest AddOutgoingRequest<TResponse>(int id)
    {
        var req = new OutgoingRequest<TResponse>();
        this.pendingOutgoingRequests.TryAdd(id, req);
        return req;
    }

    private void CancelOutgoingRequest(int id)
    {
        // Cancel the task
        if (this.pendingOutgoingRequests.TryRemove(id, out var req)) req.Cancel();

        // Send message
        var cancelMessage = TMessageAdapter.CreateCancelRequest(id);
        this.outgoingMessages.Writer.TryWrite(cancelMessage);
    }

    private void CompleteOutgoingRequest(int id, JsonElement? resultJson)
    {
        if (this.pendingOutgoingRequests.TryRemove(id, out var req))
        {
            var result = resultJson?.Deserialize(req.ResponseType, this.JsonDeserializerOptions);
            req.Complete(result);
        }
    }
    #endregion

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
