using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

internal interface IType
{
    /// <summary>
    /// The symbol of this type.
    /// </summary>
    public TypeSymbol Symbol { get; }

    /// <summary>
    /// The name of this type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The module this type is defined in.
    /// </summary>
    public IModule DeclaringModule { get; }

    /// <summary>
    /// The assembly this type is defined in.
    /// </summary>
    public IAssembly Assembly { get; }

    /// <summary>
    /// The generic parameters on this type.
    /// </summary>
    public IReadOnlyList<TypeParameterSymbol> Generics { get; }

    /// <summary>
    /// The methods on this type.
    /// </summary>
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Methods { get; }

    /// <summary>
    /// The fields on this type.
    /// </summary>
    public IReadOnlyList<FieldSymbol> Fields { get; }
}
