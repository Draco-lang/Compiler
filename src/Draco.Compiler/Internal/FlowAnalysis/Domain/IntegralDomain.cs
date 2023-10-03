using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private readonly record struct Interval(TInteger From, TInteger To)
    {
        public override string ToString() => $"[{this.From}; {this.To}]";
    }

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

        // Search for one around zero
        var span = (ReadOnlySpan<Interval>)CollectionsMarshal.AsSpan(this.subtracted);
        var (index, _) = BinarySearch.Search(span, TInteger.AdditiveIdentity, i => i.From);

        // NOTE: This is a very stupid implementation, but hopefully better than working out the edge cases
        const int surroundCheck = 1;
        var min = Math.Max(index - surroundCheck, 0);
        var max = Math.Min(index + surroundCheck, span.Length - 1);
        for (var i = max; i >= min; --i)
        {
            var interval = span[i];
            if (interval.To != this.maxValue) return interval.To + TInteger.MultiplicativeIdentity;
            if (interval.From != this.minValue) return interval.From - TInteger.MultiplicativeIdentity;
        }

        // NOTE: Should never happen
        return null;
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
        var newInterval = new Interval(from, to);

        if (span.Length == 0)
        {
            // Simple, first addition
            this.subtracted.Add(new(from, to));
            return;
        }

        var (startIndex, _) = BinarySearch.Search(span, from, i => i.From);
        var (endIndex, _) = BinarySearch.Search(span, to, i => i.To);

        // NOTE: This is a very stupid implementation, but hopefully better than working out the edge cases
        const int surroundCheck = 1;
        var startErase = -1;
        var endErase = -1;
        // Start
        {
            var min = Math.Max(startIndex - surroundCheck, 0);
            var max = Math.Min(startIndex + surroundCheck, span.Length - 1);
            for (var i = min; i <= max; ++i)
            {
                if (!Intersects(span[i], newInterval)) continue;
                startErase = i;
                break;
            }
        }
        // End
        {
            var max = Math.Min(endIndex + surroundCheck, span.Length - 1);
            var min = Math.Max(endIndex - surroundCheck, 0);
            for (var i = max; i >= min; --i)
            {
                if (!Intersects(span[i], newInterval)) continue;
                endErase = i;
                break;
            }
        }

        // If negative, no need to erase
        if (startErase < 0)
        {
            Debug.Assert(startIndex == endIndex);
            Debug.Assert(endErase < 0);
            this.subtracted.Insert(startIndex, newInterval);
            return;
        }

        Debug.Assert(endErase >= 0);

        // Expand from and to as needed
        from = Min(from, span[startErase].From);
        to = Max(to, span[endErase].To);

        // Erase
        this.subtracted.RemoveRange(startErase, endErase - startErase + 1);
        // Insert
        this.subtracted.Insert(startErase, new(from, to));
    }

    private BoundPattern ToPattern(TInteger integer) =>
        new BoundLiteralPattern(null, integer, this.backingType);

    private static bool Intersects(Interval a, Interval b) =>
           Touches(a, b)
        || Contains(a, b.From)
        || Contains(a, b.To)
        || Contains(b, a.From)
        || Contains(b, a.To);
    private static bool Contains(Interval iv, TInteger n) => iv.From <= n && n <= iv.To;
    private static bool Touches(Interval a, Interval b) =>
           a.To + TInteger.MultiplicativeIdentity == b.From
        || b.To + TInteger.MultiplicativeIdentity == a.From;
    private static TInteger Min(TInteger a, TInteger b) => a > b ? b : a;
    private static TInteger Max(TInteger a, TInteger b) => a > b ? a : b;
}
