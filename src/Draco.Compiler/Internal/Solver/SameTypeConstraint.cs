using System.Collections.Generic;
using System.Collections.Immutable;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint asserting that two types have to be exactly the same.
/// </summary>
internal sealed class SameTypeConstraint : Constraint<Unit>
{
    /// <summary>
    /// The types that all should be the same.
    /// </summary>
    public ImmutableArray<TypeSymbol> Types { get; }

    public SameTypeConstraint(ImmutableArray<TypeSymbol> types)
    {
        this.Types = types;
    }

    public override string ToString() => $"SameType({string.Join(", ", this.Types)})";
}
