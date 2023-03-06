using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents the type of a callable function.
/// </summary>
internal sealed class FunctionType
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
}
