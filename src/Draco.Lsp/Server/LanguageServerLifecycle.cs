using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamJsonRpc;
using Draco.Lsp.Model;
using System.Reflection;
using Draco.Lsp.Attributes;
using System.Diagnostics;

namespace Draco.Lsp.Server;

/// <summary>
/// Handles fundamental LSP lifecycle messages so the user does not have to.
/// </summary>
internal sealed class LanguageServerLifecycle
{
    private readonly ILanguageServer server;
    private readonly JsonRpc jsonRpc;

    public LanguageServerLifecycle(ILanguageServer server, JsonRpc jsonRpc)
    {
        this.server = server;
        this.jsonRpc = jsonRpc;
    }

    [JsonRpcMethod("initialize", UseSingleObjectParameterDeserialization = true)]
    public Task<InitializeResult> InitializeAsync(InitializedParams param) =>
        Task.FromResult(new InitializeResult()
        {
            ServerInfo = this.server.Info,
            Capabilities = this.BuildServerCapabilities(),
        });

    [JsonRpcMethod("exit", UseSingleObjectParameterDeserialization = true)]
    public Task ExitAsync()
    {
        this.jsonRpc.Dispose();
        return Task.CompletedTask;
    }

    private ServerCapabilities BuildServerCapabilities()
    {
        var capabilities = new ServerCapabilities();

        // We collect all interfaces of the server that has the capability property on it
        var capabilityInterfaces = this.server
            .GetType()
            .GetInterfaces()
            .Select(i => (Attribute: i.GetCustomAttribute<CapabilityAttribute>(), Interface: i))
            .Where(pair => pair.Attribute is not null);

        // Go through these pairs
        foreach (var (attr, @interface) in capabilityInterfaces)
        {
            Debug.Assert(attr is not null);

            // Retrieve the capability property from the server capabilities
            var serverCapabilityProp = typeof(ServerCapabilities).GetProperty(attr.Property)
                                    ?? throw new InvalidOperationException($"no capability {attr.Property} found in server capabilities");

            // Each capability interface needs to define a property called 'Capability'
            // that describes the registration options
            var interfaceCapabilityProp = @interface.GetProperty("Capability")
                                       ?? throw new InvalidOperationException($"capability interface {@interface.Name} does not define its capabilities");

            // Retrieve the capability value defined by the interface
            var capability = interfaceCapabilityProp.GetValue(this.server);
            // We can fill out the appropriate field in the server capabilities
            SetCapability(capabilities, serverCapabilityProp, capability);
        }

        return capabilities;
    }

    private static void SetCapability(ServerCapabilities capabilities, PropertyInfo prop, object? capability)
    {
        // If it's null, we can't do anything else
        if (capability is null)
        {
            prop.SetValue(capabilities, null);
            return;
        }

        var propType = prop.PropertyType;
        var capabilityType = capability.GetType();
        // Unwrap, in case either is nullable
        propType = Nullable.GetUnderlyingType(propType) ?? propType;
        capabilityType = Nullable.GetUnderlyingType(capabilityType) ?? capabilityType;

        // If they are equal, just assign
        if (propType == capabilityType)
        {
            // Convert for nullability
            prop.SetValue(capabilities, capability);
            return;
        }

        // If the property is a OneOf, we can check if the capability is any of its alternatives
        if (propType.IsAssignableTo(typeof(IOneOf)))
        {
            var genericArgs = propType.GetGenericArguments();
            if (genericArgs.Contains(capabilityType))
            {
                // Match, wrap in OneOf
                capability = Activator.CreateInstance(propType, capability);
                prop.SetValue(capabilities, capability);
                return;
            }
        }

        // Illegal
        throw new InvalidCastException($"could not assign capability of type {capabilityType.Name} to property of type {propType.Name}");
    }
}
