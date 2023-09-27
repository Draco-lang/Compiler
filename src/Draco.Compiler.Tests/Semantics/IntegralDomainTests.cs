using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.FlowAnalysis.Domain;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Tests.Semantics;

public sealed class IntegralDomainTests
{
    private static IntegralDomain<int> Interval(int min, int max) => new(null!, min, max);

    [Fact]
    public void RemovingFullDomain()
    {
        var d = Interval(-10, 10);

        d.SubtractPattern(DiscardPattern());

        Assert.True(d.IsEmpty);
        Assert.Null(d.SampleValue());
    }

    [Fact]
    public void RemovingZero()
    {
        var d = Interval(-10, 10);

        d.SubtractPattern(LiteralPattern(0, null!));

        Assert.False(d.IsEmpty);
        Assert.Equal(1, d.SampleValue());
    }

    [Fact]
    public void RemovingAllPositive()
    {
        var d = Interval(-1, 1);

        d.SubtractPattern(LiteralPattern(1, null!));

        Assert.False(d.IsEmpty);
        Assert.Equal(0, d.SampleValue());
    }

    [Fact]
    public void RemovingAllNonNegative()
    {
        var d = Interval(-1, 1);

        d.SubtractPattern(LiteralPattern(0, null!));
        d.SubtractPattern(LiteralPattern(1, null!));

        Assert.False(d.IsEmpty);
        Assert.Equal(-1, d.SampleValue());
    }

    [Fact]
    public void RemovingAll()
    {
        var d = Interval(-1, 1);

        d.SubtractPattern(LiteralPattern(-1, null!));
        d.SubtractPattern(LiteralPattern(0, null!));
        d.SubtractPattern(LiteralPattern(1, null!));

        Assert.True(d.IsEmpty);
        Assert.Null(d.SampleValue());
    }

    [Fact]
    public void RemovingAllButZero()
    {
        var d = Interval(-1, 1);

        d.SubtractPattern(LiteralPattern(1, null!));
        d.SubtractPattern(LiteralPattern(-1, null!));

        Assert.False(d.IsEmpty);
        Assert.Equal(0, d.SampleValue());
    }
}
