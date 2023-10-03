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
    /// <summary>
    /// A continuous interval of integral values.
    /// </summary>
    /// <param name="From">The lower bound (inclusive).</param>
    /// <param name="To">The upper bound (inclusive).</param>
    private readonly record struct Interval(TInteger From, TInteger To);

    public override bool IsEmpty =>
            this.subtracted.Count == 1
         && this.subtracted[0].From == this.minValue
         && this.subtracted[0].To == this.maxValue;

    private readonly TypeSymbol backingType;
    private readonly TInteger minValue;
    private readonly TInteger maxValue;
    private readonly List<Interval> subtracted;

    private IntegralDomain(
        TypeSymbol backingType,
        TInteger minValue,
        TInteger maxValue,
        List<Interval> subtracted)
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

        // TODO
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        if (this.IsEmpty) return $"[{this.minValue}; {this.maxValue}]";

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
        if (from > to) throw new ArgumentOutOfRangeException(nameof(to), "from must be less than or equal to to");

        var span = (ReadOnlySpan<Interval>)CollectionsMarshal.AsSpan(this.subtracted);

        var (startIndex, startMatch) = BinarySearch.Search(span, from, i => i.From);
        var (endIndex, endMatch) = BinarySearch.Search(span, to, i => i.To);

        // TODO
        throw new NotImplementedException();
    }

    private BoundPattern ToPattern(TInteger integer) =>
        new BoundLiteralPattern(null, integer, this.backingType);
}
