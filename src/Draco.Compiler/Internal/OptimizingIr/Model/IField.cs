using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Read-only interface of a field.
/// </summary>
internal interface IField
{
    public FieldSymbol Symbol { get; }

    /// <summary>
    /// The name of this field.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type this procedure is defined in.
    /// </summary>
    public IType DeclaringType { get; }

    /// <summary>
    /// The attributes on this field.
    /// </summary>
    public IReadOnlyList<AttributeInstance> Attributes { get; }

    /// <summary>
    /// The type of this field.
    /// </summary>
    public TypeSymbol Type { get; }
}
