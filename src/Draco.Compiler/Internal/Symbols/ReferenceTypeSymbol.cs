using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a reference type.
/// </summary>
internal sealed class ReferenceTypeSymbol : TypeSymbol
{
    /// <summary>
    /// The element type that the reference references to.
    /// </summary>
    public TypeSymbol ElementType { get; }

    public override bool IsValueType => true;
    public override Symbol? ContainingSymbol => null;

    public ReferenceTypeSymbol(TypeSymbol elementType)
    {
        this.ElementType = elementType;
    }

    public override string ToString() => $"ref {this.ElementType}";
}
