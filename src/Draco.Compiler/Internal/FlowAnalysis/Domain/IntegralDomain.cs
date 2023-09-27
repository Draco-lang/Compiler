using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis.Domain;

/// <summary>
/// Any integral number domain.
/// </summary>
/// <typeparam name="TInteger">The integral value type.</typeparam>
internal sealed class IntegralDomain<TInteger> : ValueDomain
    where TInteger : IComparisonOperators<TInteger, TInteger, bool>,
                     IAdditionOperators<TInteger, TInteger, TInteger>,
                     ISubtractionOperators<TInteger, TInteger, TInteger>,
                     IAdditiveIdentity<TInteger, TInteger>,
                     IMultiplicativeIdentity<TInteger, TInteger>
{
    public override bool IsEmpty =>
            this.subtracted.Count == 1
         && this.subtracted[0].From == this.minValue
         && this.subtracted[0].To == this.maxValue;

    private readonly TypeSymbol backingType;
    private readonly TInteger minValue;
    private readonly TInteger maxValue;
    // inclusive - inclusive
    private readonly List<(TInteger From, TInteger To)> subtracted = new();

    public IntegralDomain(TypeSymbol backingType, TInteger minValue, TInteger maxValue)
    {
        this.backingType = backingType;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public override void Subtract(BoundPattern pattern)
    {
        switch (pattern)
        {
        case BoundDiscardPattern:
            // Subtract everything
            this.SubtractRange(this.minValue, this.maxValue);
            break;
        case BoundLiteralPattern litPattern when litPattern.Type is TInteger num:
            // Subtract singular value
            this.SubtractRange(num, num);
            break;
        default:
            throw new ArgumentException("illegal pattern for integral domain", nameof(pattern));
        }
    }

    public override BoundPattern? Sample()
    {
        // Special case: entire domain covered
        if (this.IsEmpty) return null;

        // Search to the closest zero
        var span = (ReadOnlySpan<(TInteger From, TInteger To)>)CollectionsMarshal.AsSpan(this.subtracted);
        var (index, found) = BinarySearch.Search(span, TInteger.AdditiveIdentity, i => i.From);

        // If not found, we can just return the identity
        if (!found) return this.ToPattern(TInteger.AdditiveIdentity);

        // Otherwise, we need to return one of the endpoints
        if (span[index].To == this.maxValue)
        {
            // Edge, return the one below
            return this.ToPattern(span[index].From - TInteger.MultiplicativeIdentity);
        }
        return this.ToPattern(span[index].To + TInteger.MultiplicativeIdentity);
    }

    private void SubtractRange(TInteger from, TInteger to)
    {
        var span = (ReadOnlySpan<(TInteger From, TInteger To)>)CollectionsMarshal.AsSpan(this.subtracted);

        var (startIndex, _) = BinarySearch.Search(span, from, i => i.To);
        var (endIndex, _) = BinarySearch.Search(span, to, i => i.From);

        // Merge sides
        if (startIndex > 0 && span[startIndex - 1].To + TInteger.MultiplicativeIdentity == from)
        {
            --startIndex;
            from = span[startIndex].From;
        }
        if (endIndex < span.Length - 1 && span[endIndex + 1].From == to + TInteger.MultiplicativeIdentity)
        {
            ++endIndex;
            to = span[endIndex].To;
        }

        // Remove overlapping
        this.subtracted.RemoveRange(startIndex, endIndex - startIndex);

        // Insert new range
        this.subtracted.Insert(startIndex, (from, to));
    }

    private BoundPattern ToPattern(TInteger integer) =>
        new BoundLiteralPattern(null, integer, this.backingType);
}
