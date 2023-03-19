using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents an error type.
/// </summary>
internal sealed class ErrorType : Type
{
    public static ErrorType Instance { get; } = new();

    public override bool IsError => true;

    private ErrorType()
    {
    }

    public override string ToString() => "<error>";
}
