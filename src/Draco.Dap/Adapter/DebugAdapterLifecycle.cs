using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter;

/// <summary>
/// Handles fundamental DAP lifecycle messages so the user does not have to.
/// </summary>
internal sealed class DebugAdapterLifecycle : IDebugAdapterLifecycle
{
    private readonly IDebugAdapter adapter;
    private readonly DebugAdapterConnection connection;

    public DebugAdapterLifecycle(IDebugAdapter adapter, DebugAdapterConnection connection)
    {
        this.adapter = adapter;
        this.connection = connection;
    }

    public async Task<Capabilities> InitializeAsync(InitializeRequestArguments args)
    {
        await this.adapter.InitializeAsync(args);
        return this.BuildAdapterCapabilities();
    }

    private Capabilities BuildAdapterCapabilities()
    {
        var capabilities = new Capabilities();

        // We collect all properties on the adapter that have the capability annotation
        var capabilityProperties = this.adapter
            .GetType()
            .GetInterfaces()
            .SelectMany(i => i.GetProperties())
            .Select(p => (Attribute: p.GetCustomAttribute<CapabilityAttribute>(), Property: p))
            .Where(pair => pair.Attribute is not null);

        // Go through these pairs
        foreach (var (attr, interfaceCapabilityProp) in capabilityProperties)
        {
            Debug.Assert(attr is not null);

            // Retrieve the capability property from the adapter capabilities
            var adapterCapabilityProp = typeof(Capabilities).GetProperty(attr.Property)
                                     ?? throw new InvalidOperationException($"no capability {attr.Property} found in adapter capabilities");

            // Retrieve the capability value defined by the interface
            var capability = interfaceCapabilityProp.GetValue(this.adapter);
            // We can fill out the appropriate field in the adapter capabilities
            SetCapability(capabilities, adapterCapabilityProp, capability);
        }

        return capabilities;
    }

    private static void SetCapability(Capabilities capabilities, PropertyInfo prop, object? capability)
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
