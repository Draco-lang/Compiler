using System.Collections.Generic;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents a builtin type.
/// </summary>
internal sealed class BuiltinType : Type
{
    /// <summary>
    /// The underlying system type.
    /// </summary>
    public System.Type UnderylingType { get; }

    /// <summary>
    /// The name that should be shown.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Base types of this builtin type.
    /// </summary>
    public IEnumerable<BuiltinType> Bases { get; }

    public bool IsBaseType { get; }

    public BuiltinType(System.Type underylingType, string name, bool isBaseType = false, params BuiltinType[] bases)
    {
        this.UnderylingType = underylingType;
        this.Name = name;
        this.IsBaseType = isBaseType;
        this.Bases = bases;
    }

    public BuiltinType(System.Type underylingType)
        : this(underylingType, underylingType.ToString())
    {
    }

    public BuiltinType(System.Type underylingType, string name, params BuiltinType[] bases)
        : this(underylingType, underylingType.ToString(), false, bases)
    {
    }

    public override string ToString() => this.Name;
}
