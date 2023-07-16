using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Draco.Lsp.Attributes;
using Draco.Lsp.Model;

namespace Draco.Lsp.Server;

/// <summary>
/// Handles fundamental LSP lifecycle messages so the user does not have to.
/// </summary>
internal sealed class LanguageServerLifecycle : ILanguageServerLifecycle
{
    private readonly ILanguageServer server;
    private readonly LanguageServerConnection connection;

    private ClientCapabilities clientCapabilities = null!;

    public LanguageServerLifecycle(ILanguageServer server, LanguageServerConnection connection)
    {
        this.server = server;
        this.connection = connection;
    }

    public async Task<InitializeResult> InitializeAsync(InitializeParams param)
    {
        this.clientCapabilities = param.Capabilities;
        await this.server.InitializeAsync(param);
        return new InitializeResult()
        {
            ServerInfo = this.server.Info,
            Capabilities = this.BuildServerCapabilities(),
        };
    }

    public async Task InitializedAsync(InitializedParams param)
    {
        // First, we collect dynamic registration options
        var registrations = this.BuildDynamicRegistrations();

        // Then we register the collected capabilities
        await this.connection.SendRequestAsync<object?>("client/registerCapability", new RegistrationParams()
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

        // Go through each capability interface
        foreach (var (attr, capabilityInterface) in this.GetCapabilityInterfaces())
        {
            // Check, if dynamic registration is allowed by the server
            if (!this.AllowRegistration(attr, isDynamic: false)) continue;

            // Get all props with capability attribute and go through them
            foreach (var (regAttr, prop) in GetOptionsPropety<ServerCapabilityAttribute>(capabilityInterface))
            {
                // Get the property value
                var propValue = prop.GetValue(this.server);

                // If the property value is null, we don't register
                if (propValue is null) continue;

                // Non-null, set it
                // Retrieve the capability property from the server capabilities
                var serverCapabilityProp = typeof(ServerCapabilities).GetProperty(regAttr.Property)
                                        ?? throw new InvalidOperationException($"no capability {regAttr.Property} found in server capabilities");

                // Retrieve the capability value defined by the interface
                var capability = prop.GetValue(this.server);
                // We can fill out the appropriate field in the server capabilities
                SetCapability(capabilities, serverCapabilityProp, capability);
            }
        }

        return capabilities;
    }

    private IList<Registration> BuildDynamicRegistrations()
    {
        var registrations = new List<Registration>();

        // Go through each capability interface
        foreach (var (capabilityAttr, capabilityInterface) in this.GetCapabilityInterfaces())
        {
            // Check, if dynamic registration is allowed by the server
            if (!this.AllowRegistration(capabilityAttr, isDynamic: true)) continue;

            // Get all props with registration options attribute and go through them
            foreach (var (regAttr, prop) in GetOptionsPropety<RegistrationOptionsAttribute>(capabilityInterface))
            {
                // Get the property value
                var propValue = prop.GetValue(this.server);

                // If the property value is null, we don't register
                if (propValue is null) continue;

                // Non-null, set it
                // Add it as a registration
                registrations.Add(new()
                {
                    Id = $"reg_{regAttr.Method}",
                    Method = regAttr.Method,
                    RegisterOptions = JsonSerializer.SerializeToElement(propValue),
                });
            }
        }

        return registrations;
    }

    private IEnumerable<(ClientCapabilityAttribute Attribute, Type Interface)> GetCapabilityInterfaces() => this.server
        .GetType()
        .GetInterfaces()
        .Select(i => (Attribute: i.GetCustomAttribute<ClientCapabilityAttribute>(), Interface: i))
        .Where(p => p.Attribute is not null)!;

    private bool AllowRegistration(ClientCapabilityAttribute attrib, bool isDynamic)
    {
        Debug.Assert(this.clientCapabilities is not null);

        var path = attrib.Path.Split('.');

        var currentObj = this.clientCapabilities as object;
        foreach (var prop in path)
        {
            var nextObj = currentObj.GetType().GetProperty(prop)?.GetValue(currentObj);
            // No matter what, we couldn't navigate to the capability
            // The capability isn't supported either way
            if (nextObj is null) return false;
            currentObj = nextObj;
        }

        // We got to the end of the path with a non-null capability object
        // We need to check for a dynamic registration property
        var dynamicReg = currentObj.GetType().GetProperty("DynamicRegistration")?.GetValue(currentObj);
        var supportsDynamic = (dynamicReg as bool?) ?? false;

        // If supports dynamic, we only allow for the dynamic branch to succeed
        // If supports static, we only allow if it doesn't support dynamic
        return isDynamic ? supportsDynamic : !supportsDynamic;
    }

    private static IEnumerable<(TAttribute Attribute, PropertyInfo Property)> GetOptionsPropety<TAttribute>(Type capabilityInterface)
        where TAttribute : Attribute => capabilityInterface
        .GetProperties()
        .Select(p => (Attribute: p.GetCustomAttribute<TAttribute>(), Property: p))
        .Where(pair => pair.Attribute is not null)!;

    private static void SetCapability(ServerCapabilities capabilities, PropertyInfo prop, object? capability)
    {
        // If it's null, we can't do anything, and it's already null
        if (capability is null) return;

        var propType = prop.PropertyType;
        var capabilityType = capability.GetType();
        // Unwrap, in case either is nullable
        propType = Nullable.GetUnderlyingType(propType) ?? propType;
        capabilityType = Nullable.GetUnderlyingType(capabilityType) ?? capabilityType;

        // If they are assignable, just assign
        if (capabilityType.IsAssignableTo(propType))
        {
            // Convert for nullability
            prop.SetValue(capabilities, capability);
            return;
        }

        // If the property is a OneOf, we can check if the capability is any of its alternatives
        if (propType.IsAssignableTo(typeof(IOneOf)))
        {
            var genericArgs = propType.GetGenericArguments();
            if (genericArgs.Any(capabilityType.IsAssignableTo))
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
