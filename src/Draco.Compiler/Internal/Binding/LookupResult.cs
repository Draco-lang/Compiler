using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents the result of a symbol lookup.
/// </summary>
internal sealed class LookupResult
{
    /// <summary>
    /// The symbols found.
    /// </summary>
    public ImmutableArray<Symbol>.Builder Symbols { get; } = ImmutableArray.CreateBuilder<Symbol>();

    /// <summary>
    /// The error, in case the lookup failed.
    /// </summary>
    public Diagnostic? Error { get; set; }
}
