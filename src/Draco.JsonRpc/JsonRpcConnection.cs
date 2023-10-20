using System.Buffers.Text;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;
using System.Threading.Channels;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Draco.JsonRpc;

/// <summary>
/// A base class for a JSON-RPC connection.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
/// <typeparam name="TError">The error descriptor.</typeparam>
public abstract class JsonRpcConnection<TMessage, TError> : IJsonRpcConnection
{
    protected sealed class JsonRpcResponseException : Exception
    {
        public TError ResponseError { get; }

        public JsonRpcResponseException(TError error, string message)
            : base(message)
        {
            this.ResponseError = error;
        }
    }

    protected interface IOutgoingRequest
    {
        public Task Task { get; }
        public Type ResponseType { get; }

        public void Cancel();
        public void Complete(object? result);
        public void Fail(Exception exception);
    }

    private sealed class OutgoingRequest<TResponse> : IOutgoingRequest
    {
        public Task Task => this.tcs.Task;
        public Type ResponseType => typeof(TResponse);

        private readonly TaskCompletionSource<TResponse?> tcs = new();

        public void Cancel() => this.tcs.SetCanceled();
        public void Complete(object? result) => this.tcs.SetResult((TResponse?)result);
        public void Fail(Exception exception) => this.tcs.SetException(exception);
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

    // Shutdown
    private readonly CancellationTokenSource shutdownTokenSource = new();

    // Communication state
    private int lastMessageId = 0;

    public void AddHandler(IJsonRpcMethodHandler handler) => this.methodHandlers.Add(handler.MethodName, handler);

    public Task ListenAsync() => Task.WhenAll(
        this.ReaderLoopAsync(),
        this.WriterLoopAsync(),
        this.ProcessorLoopAsync());

    public void Shutdown() => this.shutdownTokenSource.Cancel();

    /// <summary>
    /// Generates a new message ID.
    /// </summary>
    /// <returns>The next free message ID.</returns>
    protected int NextMessageId() => Interlocked.Increment(ref this.lastMessageId);

    #region Message Loops
    private async Task ReaderLoopAsync()
    {
        while (true)
        {
            try
            {
                var (message, foundMessage) = await this
                    .ReadMessageAsync()
                    .WaitAsync(this.shutdownTokenSource.Token);
                if (!foundMessage) break;

                if (this.IsResponseMessage(message!))
                {
                    this.ProcessIncomingResponse(message!);
                }
                else
                {
                    await this.incomingMessages.Writer.WriteAsync(message!);
                }
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == this.shutdownTokenSource.Token)
            {
                break;
            }
            catch (JsonException ex)
            {
                var error = this.CreateJsonExceptionError(ex);
                await this.SendMessageAsync(this.CreateErrorResponseMessage(default!, error));
                continue;
            }
        }
    }

    private async Task WriterLoopAsync()
    {
        await foreach (var message in this.outgoingMessages.Reader.ReadAllAsync(this.shutdownTokenSource.Token))
        {
            await this.WriteMessageAsync(message);
        }
    }

    private async Task ProcessorLoopAsync()
    {
        bool IsMutating(TMessage message)
        {
            var method = this.GetMessageMethodName(message);
            return this.methodHandlers.TryGetValue(method, out var handler)
                && handler.Mutating;
        }

        var currentTasks = new List<Task>();
        await foreach (var message in this.incomingMessages.Reader.ReadAllAsync(this.shutdownTokenSource.Token))
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
    private async Task ProcessMessageAsync(TMessage message)
    {
        await Task.Yield();

        if (this.IsRequestMessage(message))
        {
            var response = await this.ProcessIncomingRequestAsync(message);
            await this.SendMessageAsync(response);
        }
        else if (this.IsNotificationMessage(message))
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
        var messageId = this.GetMessageId(message);
        var methodName = this.GetMessageMethodName(message);
        var @params = this.GetMessageParams(message);

        TMessage Error(TError error) => this.CreateErrorResponseMessage(message, error);

        // Custom handling
        var (customResponse, customHandled) = await this.TryProcessCustomRequest(message);
        if (customHandled) return customResponse!;

        // Error handling block
        if (!this.methodHandlers.TryGetValue(methodName, out var handler))
        {
            return Error(this.CreateHandlerNotRegisteredError(methodName));
        }
        if (@params is JsonElement { ValueKind: not (JsonValueKind.Object or JsonValueKind.Array) })
        {
            return Error(this.CreateInvalidRequestError());
        }
        if (!handler.IsRequest)
        {
            return Error(this.CreateHandlerWasRegisteredAsNotificationHandlerError(methodName));
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
                return Error(this.CreateJsonExceptionError(ex));
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
            return Error(this.CreateExceptionError(error));
        }
        else
        {
            var resultJson = JsonSerializer.SerializeToElement(result, this.JsonSerializerOptions);
            return this.CreateOkResponseMessage(message, resultJson);
        }
    }

    private void ProcessIncomingResponse(TMessage message)
    {
        var messageId = (int)this.GetMessageId(message)!;
        var @params = this.GetMessageParams(message);

        var (error, hasError) = this.GetMessageError(message);
        if (hasError)
        {
            var errorMessage = this.GetErrorMessage(error!);
            var exception = new JsonRpcResponseException(error!, errorMessage);
            this.FailOutgoingRequest(messageId, exception);
        }
        else
        {
            this.CompleteOutgoingRequest(messageId, @params);
        }
    }

    private async Task ProcessIncomingNotificationAsync(TMessage message)
    {
        var methodName = this.GetMessageMethodName(message);
        var @params = this.GetMessageParams(message);

        // Custom handling
        var customHandled = await this.TryProcessCustomNotification(message);
        if (customHandled) return;

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

    protected abstract Task<(TMessage? Message, bool Handled)> TryProcessCustomRequest(TMessage message);
    protected abstract Task<bool> TryProcessCustomNotification(TMessage message);
    #endregion

    #region Sending Message
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params) =>
        this.SendRequestAsync<TResponse>(method, @params, CancellationToken.None);
    public Task<TResponse?> SendRequestAsync<TResponse>(string method, object? @params, CancellationToken cancellationToken)
    {
        // Construct request
        var id = this.NextMessageId();
        var serializedParams = JsonSerializer.SerializeToElement(@params, this.JsonSerializerOptions);
        var request = this.CreateRequestMessage(id, method, serializedParams);

        // Add the request to pending
        var pendingReq = this.AddOutgoingRequest<TResponse>(id);

        // When canceled, cancel corresponding TCS and write cancel message
        cancellationToken.Register(() => this.CancelOutgoingRequest(id));

        // Actually send message
        this.SendMessage(request);
        return (Task<TResponse?>)pendingReq.Task;
    }

    public async Task SendNotificationAsync(string method, object? @params)
    {
        var serializedParams = JsonSerializer.SerializeToElement(@params, this.JsonSerializerOptions);
        var notification = this.CreateNotificationMessage(method, serializedParams);
        await this.SendMessageAsync(notification);
    }

    protected Task SendMessageAsync(TMessage message) =>
        this.outgoingMessages.Writer.WriteAsync(message).AsTask();

    protected void SendMessage(TMessage message) =>
        this.outgoingMessages.Writer.TryWrite(message);
    #endregion

    #region Request Response
    protected CancellationToken AddIncomingRequest(object id)
    {
        var cts = new CancellationTokenSource();
        this.pendingIncomingRequests.TryAdd(id, cts);
        return cts.Token;
    }

    protected void CancelIncomingRequest(object id)
    {
        if (this.pendingIncomingRequests.TryRemove(id, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    protected void CompleteIncomingRequest(object id)
    {
        if (this.pendingIncomingRequests.TryRemove(id, out var cts))
        {
            cts.Dispose();
        }
    }

    protected IOutgoingRequest AddOutgoingRequest<TResponse>(int id)
    {
        var req = new OutgoingRequest<TResponse>();
        this.pendingOutgoingRequests.TryAdd(id, req);
        return req;
    }

    protected virtual void CancelOutgoingRequest(int id)
    {
        // Cancel the task
        if (this.pendingOutgoingRequests.TryRemove(id, out var req))
        {
            req.Cancel();
        }
    }

    protected void CompleteOutgoingRequest(int id, JsonElement? resultJson)
    {
        if (this.pendingOutgoingRequests.TryRemove(id, out var req))
        {
            var result = resultJson?.Deserialize(req.ResponseType, this.JsonDeserializerOptions);
            req.Complete(result);
        }
    }

    protected void FailOutgoingRequest(int id, JsonRpcResponseException error)
    {
        if (this.pendingOutgoingRequests.TryRemove(id, out var req))
        {
            req.Fail(error);
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

    #region Factory Methods
    protected abstract TMessage CreateRequestMessage(int id, string method, JsonElement @params);
    protected abstract TMessage CreateOkResponseMessage(TMessage request, JsonElement okResult);
    protected abstract TMessage CreateErrorResponseMessage(TMessage request, TError errorResult);
    protected abstract TMessage CreateNotificationMessage(string method, JsonElement @params);

    protected abstract TError CreateExceptionError(Exception exception);
    protected abstract TError CreateJsonExceptionError(JsonException exception);
    protected abstract TError CreateHandlerNotRegisteredError(string method);
    protected abstract TError CreateInvalidRequestError();
    protected abstract TError CreateHandlerWasRegisteredAsNotificationHandlerError(string method);
    #endregion

    #region Observers
    protected abstract bool IsRequestMessage(TMessage message);
    protected abstract bool IsResponseMessage(TMessage message);
    protected abstract bool IsNotificationMessage(TMessage message);
    protected abstract object? GetMessageId(TMessage message);
    protected abstract string GetMessageMethodName(TMessage message);
    protected abstract JsonElement? GetMessageParams(TMessage message);
    protected abstract (TError? Error, bool HasError) GetMessageError(TMessage message);
    protected abstract string GetErrorMessage(TError error);
    #endregion
}
