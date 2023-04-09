using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents the type of a callable function.
/// </summary>
internal sealed class FunctionType : Type
{
    /// <summary>
    /// The parameter types of the function.
    /// </summary>
    public ImmutableArray<Type> ParameterTypes { get; }

    /// <summary>
    /// The return type of the function.
    /// </summary>
    public Type ReturnType { get; }

    public FunctionType(ImmutableArray<Type> parameterTypes, Type returnType)
    {
        this.ParameterTypes = parameterTypes;
        this.ReturnType = returnType;
    }

    public override string ToString() => $"({string.Join(", ", this.ParameterTypes)}) -> {this.ReturnType}";

    public override bool ContainsTypeVariable(TypeVariable variable)
    {
        foreach (var parameter in this.ParameterTypes)
        {
            if (ReferenceEquals(variable, parameter)) return true;
        }
        return ReferenceEquals(this.ReturnType, variable);
    }
}
