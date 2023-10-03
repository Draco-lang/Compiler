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
    where TInteger : struct,
                     IComparisonOperators<TInteger, TInteger, bool>,
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
    private readonly List<(TInteger From, TInteger To)> subtracted;

    private IntegralDomain(
        TypeSymbol backingType,
        TInteger minValue,
        TInteger maxValue,
        List<(TInteger From, TInteger To)> subtracted)
    {
        this.backingType = backingType;
        this.minValue = minValue;
        this.maxValue = maxValue;
        this.subtracted = subtracted;
    }

    public IntegralDomain(TypeSymbol backingType, TInteger minValue, TInteger maxValue)
        : this(backingType, minValue, maxValue, new())
    {
    }

    public override ValueDomain Clone() =>
        new IntegralDomain<TInteger>(this.backingType, this.minValue, this.maxValue, this.subtracted.ToList());

    public override void SubtractPattern(BoundPattern pattern)
    {
        switch (pattern)
        {
        case BoundDiscardPattern:
            // Subtract everything
            this.SubtractRange(this.minValue, this.maxValue);
            break;
        case BoundLiteralPattern litPattern when litPattern.Value is TInteger num:
            // Subtract singular value
            this.SubtractRange(num, num);
            break;
        default:
            throw new ArgumentException("illegal pattern for integral domain", nameof(pattern));
        }
    }

    public override BoundPattern? SamplePattern()
    {
        var sample = this.SampleValue();
        return sample is null ? null : this.ToPattern(sample.Value);
    }

    public TInteger? SampleValue()
    {
        // Special case: entire domain covered
        if (this.IsEmpty) return null;

        // Search to the closest zero
        var span = (ReadOnlySpan<(TInteger From, TInteger To)>)CollectionsMarshal.AsSpan(this.subtracted);
        var (index, found) = BinarySearch.Search(span, TInteger.AdditiveIdentity, i => i.From);

        // TODO: Not correct
        // If not found, we can just return the identity
        if (!found) return TInteger.AdditiveIdentity;

        // Otherwise, we need to return one of the endpoints
        if (span[index].To == this.maxValue)
        {
            // Edge, return the one below
            return span[index].From - TInteger.MultiplicativeIdentity;
        }
        return span[index].To + TInteger.MultiplicativeIdentity;
    }

    public override string ToString()
    {
        if (this.IsEmpty) return "empty";

        var parts = new List<string>();
        if (this.subtracted[0].From != this.minValue) parts.Add($"[{this.minValue}; {this.subtracted[0].From})");
        for (var i = 0; i < this.subtracted.Count - 1; ++i)
        {
            parts.Add($"({this.subtracted[i].To}; {this.subtracted[i + 1].From})");
        }
        if (this.subtracted[^1].To != this.maxValue) parts.Add($"({this.subtracted[^1].To}; {this.maxValue}]");
        return string.Join(" U ", parts);
    }

    private void SubtractRange(TInteger from, TInteger to)
    {
        var span = (ReadOnlySpan<(TInteger From, TInteger To)>)CollectionsMarshal.AsSpan(this.subtracted);

        var (startIndex, startMatch) = BinarySearch.Search(span, from, i => i.To);
        var (endIndex, endMatch) = BinarySearch.Search(span, to, i => i.From);

        // TODO: NOT CORRECT
        // Merge sides
        if (startIndex > 0 && span[startIndex - 1].To + TInteger.MultiplicativeIdentity == from)
        {
            --startIndex;
            from = span[startIndex].From;
        }
        // TODO: NOT CORRECT
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
