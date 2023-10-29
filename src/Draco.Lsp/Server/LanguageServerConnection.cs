using System;
using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Draco.JsonRpc;
using Draco.Lsp.Model;
using Draco.Lsp.Serialization;

using LspMessage = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RequestMessage, Draco.Lsp.Model.NotificationMessage, Draco.Lsp.Model.ResponseMessage>;

namespace Draco.Lsp.Server;

internal sealed class LanguageServerConnection : JsonRpcConnection<LspMessage, ResponseError>
{
    public override IDuplexPipe Transport { get; }

    public override JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        Converters =
        {
            new OneOfConverter(),
            new TupleConverter(),
            new UriConverter(),
        }
    };

    public override JsonSerializerOptions JsonDeserializerOptions { get; } = new()
    {
        Converters =
        {
            new OneOfConverter(),
            new TupleConverter(),
            new UriConverter(),
            new ModelInterfaceConverter(),
        }
    };

    public LanguageServerConnection(IDuplexPipe transport)
    {
        this.Transport = transport;
    }

    protected override Task<(LspMessage Message, bool Handled)> TryProcessCustomRequest(LspMessage message) =>
        Task.FromResult((message, false));

    protected override Task<bool> TryProcessCustomNotification(LspMessage message)
    {
        // Cancellation
        var method = this.GetMessageMethodName(message);
        if (method == "$/cancelRequest")
        {
            var id = (message
                .As<NotificationMessage>()
                .Params?
                .Deserialize<CancelParams>(this.JsonDeserializerOptions)?
                .Id as IOneOf)?.Value;
            if (id is not null)
            {
                this.CancelIncomingRequest(id);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    protected override void CancelOutgoingRequest(int id)
    {
        base.CancelOutgoingRequest(id);
        this.SendMessage(this.CreateNotificationMessage(
            "$/cancelRequest",
            JsonSerializer.SerializeToElement(new CancelParams
            {
                Id = id,
            }, this.JsonSerializerOptions)));
    }

    protected override LspMessage CreateRequestMessage(int id, string method, JsonElement @params) => new RequestMessage
    {
        Jsonrpc = "2.0",
        Method = method,
        Id = id,
        Params = @params,
    };
    protected override LspMessage CreateOkResponseMessage(LspMessage request, JsonElement okResult) => new ResponseMessage
    {
        Jsonrpc = "2.0",
        Id = ToMessageId(this.GetMessageId(request)!),
        Result = okResult,
    };
    protected override LspMessage CreateErrorResponseMessage(LspMessage request, ResponseError errorResult) => new ResponseMessage
    {
        Jsonrpc = "2.0",
        Id = ToMessageId(this.GetMessageId(request)!),
        Error = errorResult,
    };
    protected override LspMessage CreateNotificationMessage(string method, JsonElement @params) => new NotificationMessage
    {
        Jsonrpc = "2.0",
        Method = method,
        Params = @params,
    };

    private static OneOf<int, string> ToMessageId(object id) => id switch
    {
        int i => i,
        string s => s,
        OneOf<int, string> o => o,
        _ => -1,
    };

    protected override ResponseError CreateExceptionError(Exception exception)
    {
        // Unwrap
        if (exception is TargetInvocationException) exception = exception.InnerException!;
        if (exception is JsonException jsonException) return this.CreateJsonExceptionError(jsonException);

        var errorCode = exception switch
        {
            // RequestCancelled
            OperationCanceledException => -32800,
            // InternalError
            _ => -32603,
        };

        return new()
        {
            Message = exception.Message,
            Code = errorCode,
            Data = JsonSerializer.SerializeToElement(exception?.ToString(), this.JsonSerializerOptions),
        };
    }
    protected override ResponseError CreateHandlerNotRegisteredError(string method) => new()
    {
        // MethodNotFound
        Code = -32601,
        Message = $"A handler for the method '{method}' was not registered.",
    };
    protected override ResponseError CreateHandlerWasRegisteredAsNotificationHandlerError(string method) => new()
    {
        // InternalError
        Code = -32603,
        Message = $"A handler for the method '{method}' was registered as a notification handler.",
    };
    protected override ResponseError CreateInvalidRequestError() => new()
    {
        // InvalidRequest
        Code = -32600,
        Message = "Invalid request.",
    };
    protected override ResponseError CreateJsonExceptionError(JsonException exception)
    {
        // Typically, only exceptions from Utf8JsonReader have the position info set
        // So, we assume this is a parse error if it's there, and other errors are serialization errors
        var code = exception.BytePositionInLine.HasValue
            ? -32700  // ParseError
            : -32602; // InvalidParams

        var responseError = new ResponseError
        {
            Message = exception.Message,
            Data = JsonSerializer.SerializeToElement(exception.ToString(), this.JsonSerializerOptions),
            Code = code,
        };

        return responseError;
    }

    protected override bool IsRequestMessage(LspMessage message) => message.Is<RequestMessage>();
    protected override bool IsResponseMessage(LspMessage message) => message.Is<ResponseMessage>();
    protected override bool IsNotificationMessage(LspMessage message) => message.Is<NotificationMessage>();

    protected override object? GetMessageId(LspMessage message)
    {
        static object? GetRawId(LspMessage message)
        {
            if (message.Is<RequestMessage>(out var req)) return req.Id;
            if (message.Is<ResponseMessage>(out var resp)) return resp.Id;
            return null;
        }

        var rawId = GetRawId(message);
        return (rawId as IOneOf)?.Value;
    }
    protected override string GetMessageMethodName(LspMessage message)
    {
        if (message.Is<RequestMessage>(out var req)) return req.Method;
        if (message.Is<NotificationMessage>(out var notif)) return notif.Method;
        return string.Empty;
    }
    protected override JsonElement? GetMessageParams(LspMessage message)
    {
        if (message.Is<RequestMessage>(out var req)) return req.Params;
        if (message.Is<ResponseMessage>(out var resp)) return resp.Result;
        if (message.Is<NotificationMessage>(out var notif)) return notif.Params;
        return null;
    }
    protected override (ResponseError? Error, bool HasError) GetMessageError(LspMessage message)
    {
        if (message.Is<ResponseMessage>(out var resp)) return (resp.Error, resp.Error is not null);
        return (null, false);
    }
    protected override string GetErrorMessage(ResponseError error) => error.Message;
}
