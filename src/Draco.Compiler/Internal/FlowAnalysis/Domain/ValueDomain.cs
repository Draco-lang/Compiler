using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.FlowAnalysis.Domain;

/// <summary>
/// Represents some domain of values in the compiler.
/// </summary>
internal abstract class ValueDomain
{
    // TODO: Docs
    public static ValueDomain CreateDomain(IntrinsicSymbols intrinsics, TypeSymbol type)
    {
        if (SymbolEqualityComparer.Default.Equals(type, intrinsics.Int32))
        {
            return new IntegralDomain<int>(type, int.MinValue, int.MaxValue);
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    /// <summary>
    /// Removes the given pattern from the domain.
    /// </summary>
    /// <param name="pattern">The pattern to remove.</param>
    public abstract void Subtract(BoundPattern pattern);

    /// <summary>
    /// Retrieves a sample value from the domain that has not been covered yet.
    /// </summary>
    /// <returns>A pattern representing an uncovered value, or null, if the domain has been emptied.</returns>
    public abstract BoundPattern? Sample();
}
