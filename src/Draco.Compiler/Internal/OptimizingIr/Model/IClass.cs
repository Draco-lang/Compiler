using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Interface of a class definition.
/// </summary>
internal interface IClass
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
    /// The generic parameters on this type.
    /// </summary>
    public IReadOnlyList<TypeParameterSymbol> Generics { get; }

    /// <summary>
    /// The fields on this type.
    /// </summary>
    public IReadOnlyList<FieldSymbol> Fields { get; }

    /// <summary>
    /// The procedures on this type.
    /// </summary>
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures { get; }
}
