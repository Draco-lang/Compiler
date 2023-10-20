using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Draco.Dap.Model;
using Draco.Dap.Serialization;
using Draco.JsonRpc;
using DapMessage = Draco.Dap.Model.OneOf<Draco.Dap.Model.RequestMessage, Draco.Dap.Model.EventMessage, Draco.Dap.Model.ResponseMessage>;

namespace Draco.Dap.Adapter;

internal sealed class DebugAdapterConnection : JsonRpcConnection<DapMessage, ErrorResponse>
{
    public override IDuplexPipe Transport { get; }

    public override JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        Converters =
        {
            new OneOfConverter(),
        }
    };

    public override JsonSerializerOptions JsonDeserializerOptions { get; } = new()
    {
        Converters =
        {
            new OneOfConverter(),
        }
    };

    public DebugAdapterConnection(IDuplexPipe transport)
    {
        this.Transport = transport;
    }

    protected override Task<(DapMessage Message, bool Handled)> TryProcessCustomRequest(DapMessage message) =>
        Task.FromResult((message, false));

    protected override Task<bool> TryProcessCustomNotification(DapMessage message) =>
        Task.FromResult(false);

    protected override DapMessage CreateRequestMessage(int id, string method, JsonElement @params) => new RequestMessage
    {
        Command = method,
        Arguments = @params,
        SequenceNumber = id,
    };
    protected override DapMessage CreateOkResponseMessage(DapMessage request, JsonElement okResult) => new ResponseMessage
    {
        Command = this.GetMessageMethodName(request),
        RequestSequenceNumber = (int)this.GetMessageId(request)!,
        SequenceNumber = this.NextMessageId(),
        Success = true,
        Body = okResult,
    };
    protected override DapMessage CreateErrorResponseMessage(DapMessage request, ErrorResponse errorResult) => new ResponseMessage
    {
        Command = this.GetMessageMethodName(request),
        RequestSequenceNumber = (int)this.GetMessageId(request)!,
        SequenceNumber = this.NextMessageId(),
        Success = false,
        Body = JsonSerializer.SerializeToElement(errorResult, this.JsonSerializerOptions),
    };
    protected override DapMessage CreateNotificationMessage(string method, JsonElement @params) => new EventMessage
    {
        Event = method,
        SequenceNumber = this.NextMessageId(),
        Body = @params,
    };

    protected override ErrorResponse CreateExceptionError(Exception exception)
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
            Error = new()
            {
                Id = errorCode,
                Format = exception.Message,
            },
        };
    }
    protected override ErrorResponse CreateHandlerNotRegisteredError(string method) => new()
    {
        Error = new()
        {
            Id = -32601,
            Format = "Handler for method '{method}' not registered.",
            Variables = new Dictionary<string, string>()
            {
                ["method"] = method,
            },
        },
    };
    protected override ErrorResponse CreateHandlerWasRegisteredAsNotificationHandlerError(string method) => new()
    {
        Error = new()
        {
            Id = -32603,
            Format = "Handler for method '{method}' was registered as notification handler.",
            Variables = new Dictionary<string, string>()
            {
                ["method"] = method,
            },
        },
    };
    protected override ErrorResponse CreateInvalidRequestError() => new()
    {
        Error = new()
        {
            Id = -32600,
            Format = "Invalid request.",
        },
    };
    protected override ErrorResponse CreateJsonExceptionError(JsonException exception)
    {
        // Typically, only exceptions from Utf8JsonReader have the position info set
        // So, we assume this is a parse error if it's there, and other errors are serialization errors
        var code = exception.BytePositionInLine.HasValue
            ? -32700  // ParseError
            : -32602; // InvalidParams

        return new()
        {
            Error = new()
            {
                Id = code,
                Format = exception.Message,
            },
        };
    }

    protected override bool IsRequestMessage(DapMessage message) => message.Is<RequestMessage>();
    protected override bool IsResponseMessage(DapMessage message) => message.Is<ResponseMessage>();
    protected override bool IsNotificationMessage(DapMessage message) => message.Is<EventMessage>();

    protected override object? GetMessageId(DapMessage message)
    {
        if (message.Is<RequestMessage>(out var req)) return req.SequenceNumber;
        if (message.Is<ResponseMessage>(out var resp)) return resp.SequenceNumber;
        if (message.Is<EventMessage>(out var evt)) return evt.SequenceNumber;
        return null;
    }
    protected override string GetMessageMethodName(DapMessage message)
    {
        if (message.Is<RequestMessage>(out var req)) return req.Command;
        if (message.Is<ResponseMessage>(out var resp)) return resp.Command;
        if (message.Is<EventMessage>(out var evt)) return evt.Event;
        return string.Empty;
    }
    protected override JsonElement? GetMessageParams(DapMessage message)
    {
        if (message.Is<RequestMessage>(out var req)) return req.Arguments;
        if (message.Is<ResponseMessage>(out var resp)) return resp.Body;
        if (message.Is<EventMessage>(out var evt)) return evt.Body;
        return default;
    }
    protected override (ErrorResponse? Error, bool HasError) GetMessageError(DapMessage message)
    {
        if (message.Is<ResponseMessage>(out var resp) && !resp.Success)
        {
            var error = JsonSerializer.Deserialize<ErrorResponse>(resp.Body!.Value, this.JsonDeserializerOptions);
            return (error, true);
        }

        return (null, false);
    }
    protected override string GetErrorMessage(ErrorResponse error) => error.GetMessage() ?? string.Empty;
}
