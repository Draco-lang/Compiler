using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Interface of a class definition.
/// </summary>
internal interface IClass
{
    /// <summary>
    /// The symbol of this class.
    /// </summary>
    public TypeSymbol Symbol { get; }

    /// <summary>
    /// The name of this class.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The generic parameters on this class.
    /// </summary>
    public IReadOnlyList<TypeParameterSymbol> Generics { get; }

    // TODO: Fields and props should be order-dependent for classes and modules too
    /// <summary>
    /// The fields on this class.
    /// </summary>
    public IReadOnlySet<FieldSymbol> Fields { get; }

    /// <summary>
    /// The properties within this class.
    /// </summary>
    public IReadOnlySet<PropertySymbol> Properties { get; }

    /// <summary>
    /// The procedures on this class.
    /// </summary>
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures { get; }
}
