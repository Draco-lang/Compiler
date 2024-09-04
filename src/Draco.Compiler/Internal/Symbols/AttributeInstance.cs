using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// An attribute instance attached to a symbol.
/// </summary>
internal sealed class AttributeInstance(
    FunctionSymbol constructor,
    ImmutableArray<ConstantValue> fixedArguments,
    ImmutableDictionary<string, ConstantValue> namedArguments)
{
    /// <summary>
    /// The attribute constructor.
    /// </summary>
    public FunctionSymbol Constructor { get; } = constructor;

    /// <summary>
    /// The fixed arguments of the attribute.
    /// </summary>
    public ImmutableArray<ConstantValue> FixedArguments { get; } = fixedArguments;

    /// <summary>
    /// The named arguments of the attribute.
    /// </summary>
    public ImmutableDictionary<string, ConstantValue> NamedArguments { get; } = namedArguments;

    /// <summary>
    /// Translates this attribute instance to an attribute of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of attribute to translate to.</typeparam>
    /// <returns>The attribute instance as an attribute of type <typeparamref name="T"/>.</returns>
    public T ToAttribute<T>()
        where T : System.Attribute
    {
        // Search for the constructor
        var ctorInfo = typeof(T).GetConstructors().FirstOrDefault(this.SignatureMatches);
        if (ctorInfo is null) throw new System.ArgumentException("no matching constructor found");

        // Instantiate
        var instance = (T)ctorInfo.Invoke(this.FixedArguments.Select(arg => arg.Value).ToArray());

        // Fill in properties with named arguments
        foreach (var (argName, argValue) in this.NamedArguments)
        {
            var propInfo = typeof(T).GetProperty(argName);
            if (propInfo is null) throw new System.ArgumentException("no matching property found");

            propInfo.SetValue(instance, argValue.Value);
        }

        return instance;
    }

    private bool SignatureMatches(ConstructorInfo ctorInfo)
    {
        var ctorParams = ctorInfo.GetParameters();
        if (this.Constructor.Parameters.Length != ctorParams.Length) return false;

        // Compare arguments sequentially
        for (var i = 0; i < this.Constructor.Parameters.Length; i++)
        {
            if (this.Constructor.Parameters[i].Name != ctorParams[i].Name) return false;
            var ctorParamType = this.TranslateType(ctorParams[i].ParameterType);

            if (!SymbolEqualityComparer.Default.Equals(ctorParamType, this.Constructor.Parameters[i].Type)) return false;
        }

        return true;
    }

    // TODO: We have a lot of type translation code lying around, we should probably centralize it
    private TypeSymbol TranslateType(System.Type type)
    {
        var wellKnownTypes = this.Constructor.DeclaringCompilation?.WellKnownTypes;
        if (wellKnownTypes is null) throw new System.NotImplementedException();

        if (type == typeof(byte)) return wellKnownTypes.SystemByte;
        if (type == typeof(ushort)) return wellKnownTypes.SystemUInt16;
        if (type == typeof(uint)) return wellKnownTypes.SystemUInt32;
        if (type == typeof(ulong)) return wellKnownTypes.SystemUInt64;
        if (type == typeof(sbyte)) return wellKnownTypes.SystemSByte;
        if (type == typeof(short)) return wellKnownTypes.SystemInt16;
        if (type == typeof(int)) return wellKnownTypes.SystemInt32;
        if (type == typeof(long)) return wellKnownTypes.SystemInt64;

        if (type == typeof(bool)) return wellKnownTypes.SystemBoolean;
        if (type == typeof(char)) return wellKnownTypes.SystemChar;

        if (type == typeof(string)) return wellKnownTypes.SystemString;

        if (type.IsEnum) return this.TranslateType(System.Enum.GetUnderlyingType(type));

        throw new System.NotImplementedException();
    }
}
