using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;
using Draco.Lsp.Protocol;

namespace Draco.Lsp.Server;

/// <summary>
/// Handles fundamental LSP lifecycle messages so the user does not have to.
/// </summary>
internal sealed class LanguageServerLifecycle : ILanguageServerLifecycle
{
    private readonly ILanguageServer server;
    private readonly LspConnection connection;

    public LanguageServerLifecycle(ILanguageServer server, LspConnection connection)
    {
        this.server = server;
        this.connection = connection;
    }

    public Task<InitializeResult> InitializeAsync(InitializedParams param) =>
        Task.FromResult(new InitializeResult()
        {
            ServerInfo = this.server.Info,
            Capabilities = this.BuildServerCapabilities(),
        });

    public async Task InitializedAsync(InitializedParams param)
    {
        // First, we collect dynamic registration options
        var registrations = this.BuildDynamicRegistrations();

        // Then we register the collected capabilities
        await this.connection.SendRequestAsync<object>("client/registerCapability", new RegistrationParams()
        {
            Registrations = registrations,
        });

        // Finally, we let the user implementation know
        await this.server.InitializedAsync(param);
    }

    public Task ExitAsync()
    {
        this.connection.Shutdown();
        return Task.CompletedTask;
    }

    private ServerCapabilities BuildServerCapabilities()
    {
        var capabilities = new ServerCapabilities();

        // We collect all properties on the server that have the capability annotation
        var capabilityProperties = this.server
            .GetType()
            .GetInterfaces()
            .SelectMany(i => i.GetProperties())
            .Select(p => (Attribute: p.GetCustomAttribute<CapabilityAttribute>(), Property: p))
            .Where(pair => pair.Attribute is not null);

        // Go through these pairs
        foreach (var (attr, interfaceCapabilityProp) in capabilityProperties)
        {
            Debug.Assert(attr is not null);

            // Retrieve the capability property from the server capabilities
            var serverCapabilityProp = typeof(ServerCapabilities).GetProperty(attr.Property)
                                    ?? throw new InvalidOperationException($"no capability {attr.Property} found in server capabilities");

            // Retrieve the capability value defined by the interface
            var capability = interfaceCapabilityProp.GetValue(this.server);
            // We can fill out the appropriate field in the server capabilities
            SetCapability(capabilities, serverCapabilityProp, capability);
        }

        return capabilities;
    }

    private IList<Registration> BuildDynamicRegistrations()
    {
        var registrations = new List<Registration>();

        // We collect all properties with the registration options attribute
        var registrationOptionsProps = this.server
            .GetType()
            .GetInterfaces()
            .SelectMany(i => i.GetProperties())
            .Select(p => (Attribute: p.GetCustomAttribute<RegistrationOptionsAttribute>(), Property: p))
            .Where(pair => pair.Attribute is not null);

        // Go through these properties
        foreach (var (attr, prop) in registrationOptionsProps)
        {
            Debug.Assert(attr is not null);

            // Get the property value
            var propValue = prop.GetValue(this.server);

            // If the property value is null, we don't register
            if (propValue is null) continue;

            // Add it as a registration
            registrations.Add(new()
            {
                Id = $"reg_{attr.Method}",
                Method = attr.Method,
                RegisterOptions = JsonSerializer.SerializeToElement(propValue),
            });
        }

        return registrations;
    }

    private static void SetCapability(ServerCapabilities capabilities, PropertyInfo prop, object? capability)
    {
        // If it's null, we can't do anything, and it's already null
        if (capability is null) return;

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
