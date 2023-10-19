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
using Draco.JsonRpc;
using Draco.Lsp.Model;
using Draco.Lsp.Serialization;

using LspMessage = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RequestMessage, Draco.Lsp.Model.NotificationMessage, Draco.Lsp.Model.ResponseMessage>;

namespace Draco.Lsp.Server;

internal sealed class LspMessageAdapter : IJsonRpcMessageAdapter<LspMessage, ResponseError>
{
    private LspMessageAdapter()
    {
    }

    public static LspMessage CreateRequest(int id, string method, JsonElement @params) => new RequestMessage
    {
        Jsonrpc = "2.0",
        Method = method,
        Id = id,
        Params = @params,
    };
    public static LspMessage CreateCancelRequest(int id) => throw new NotImplementedException();
    public static LspMessage CreateOkResponse(object id, JsonElement okResult) => new ResponseMessage
    {
        Jsonrpc = "2.0",
        Id = ToId(id),
        Result = okResult,
    };
    public static LspMessage CreateErrorResponse(object id, ResponseError errorResult) => new ResponseMessage
    {
        Jsonrpc = "2.0",
        Id = ToId(id),
        Error = (ResponseError)errorResult,
    };
    public static LspMessage CreateNotification(string method, JsonElement @params) => new NotificationMessage
    {
        Jsonrpc = "2.0",
        Method = method,
        Params = @params,
    };

    private static OneOf<int, string> ToId(object id) => id switch
    {
        int i => i,
        string s => s,
        OneOf<int, string> o => o,
        _ => -1,
    };

    public static ResponseError CreateExceptionError(Exception exception)
    {
        // Unwrap
        if (exception is TargetInvocationException) exception = exception.InnerException!;
        if (exception is JsonException jsonException) return CreateJsonExceptionError(jsonException);

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
            Data = JsonSerializer.SerializeToElement(exception?.ToString()),
        };
    }
    public static ResponseError CreateHandlerNotRegisteredError(string method) => new()
    {
        // MethodNotFound
        Code = -32601,
        Message = $"A handler for the method '{method}' was not registered.",
    };
    public static ResponseError CreateHandlerWasRegisteredAsNotificationHandlerError(string method) => new()
    {
        // InternalError
        Code = -32603,
        Message = $"A handler for the method '{method}' was registered as a notification handler.",
    };
    public static ResponseError CreateInvalidRequestError() => new()
    {
        // InvalidRequest
        Code = -32600,
        Message = "Invalid request.",
    };
    public static ResponseError CreateJsonExceptionError(JsonException exception)
    {
        // Typically, only exceptions from Utf8JsonReader have the position info set
        // So, we assume this is a parse error if it's there, and other errors are serialization errors
        var code = exception.BytePositionInLine.HasValue
            ? -32700  // ParseError
            : -32602; // InvalidParams

        var responseError = new ResponseError
        {
            Message = exception.Message,
            Data = JsonSerializer.SerializeToElement(exception.ToString()),
            Code = code,
        };

        return responseError;
    }

    public static bool IsRequest(LspMessage message) => message.Is<RequestMessage>();
    public static bool IsResponse(LspMessage message) => message.Is<ResponseMessage>();
    public static bool IsNotification(LspMessage message) => message.Is<NotificationMessage>();
    public static bool IsCancellation(LspMessage message) =>
           message.Is<RequestMessage>(out var req)
        && req.Method == "$/cancelRequest";

    public static object? GetId(LspMessage message)
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
    public static string GetMethodName(LspMessage message)
    {
        if (message.Is<RequestMessage>(out var req)) return req.Method;
        if (message.Is<NotificationMessage>(out var notif)) return notif.Method;
        return string.Empty;
    }
    public static JsonElement? GetParams(LspMessage message)
    {
        if (message.Is<RequestMessage>(out var req)) return req.Params;
        if (message.Is<NotificationMessage>(out var notif)) return notif.Params;
        return null;
    }
}

internal sealed class LanguageServerConnection : JsonRpcConnection<LspMessage, ResponseError, LspMessageAdapter>
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
}

internal sealed class LspResponseException : Exception
{
    public LspResponseException(ResponseError error) : base(error.Message)
    {
        this.ResponseError = error;
    }

    public ResponseError ResponseError { get; }
}
