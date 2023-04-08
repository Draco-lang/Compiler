using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Represents a type of some type-checking error. Acts as a sentinel value, absorbs cascading errors.
/// </summary>
internal sealed class ErrorTypeSymbol : TypeSymbol
{
    /// <summary>
    /// A singleton instance.
    /// </summary>
    public static ErrorTypeSymbol Instance { get; } = new();

    public override bool IsError => true;
    public override Symbol? ContainingSymbol => null;

    private ErrorTypeSymbol()
    {
    }

    public override string ToString() => "<error>";
}
