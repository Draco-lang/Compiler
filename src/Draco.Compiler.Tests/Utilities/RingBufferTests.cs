using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Tests.Utilities;

public sealed class RingBufferTests
{
    private static RingBuffer<T> MakeRingBuffer<T>(params T[] items)
    {
        var result = new RingBuffer<T>();
        foreach (var item in items) result.AddBack(item);
        return result;
    }

    [Fact]
    public void EmptyBuffer()
    {
        var rb = new RingBuffer<int>();

        Assert.Empty(rb);
        Assert.Equal(0, rb.Head);
        Assert.Equal(0, rb.Tail);
    }

    [Fact]
    public void IndexEmptyBuffer()
    {
        var rb = new RingBuffer<int>();

        Assert.Throws<ArgumentOutOfRangeException>(() => rb[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => rb[0] = 0);
    }

    [Fact]
    public void AddingElementToBack()
    {
        var rb = new RingBuffer<int>();

        rb.AddBack(1);

        Assert.Single(rb);
        Assert.Equal(0, rb.Head);
        Assert.Equal(1, rb.Tail);
        Assert.Equal(1, rb[0]);
    }

    [Fact]
    public void AddingElementToFront()
    {
        var rb = new RingBuffer<int>();

        rb.AddFront(1);

        Assert.Single(rb);
        Assert.Equal(rb.Capacity - 1, rb.Head);
        Assert.Equal(0, rb.Tail);
        Assert.Equal(1, rb[0]);
    }

    [Fact]
    public void AddingElementToFrontAndBack()
    {
        var rb = new RingBuffer<int>();

        rb.AddFront(2);
        rb.AddBack(1);

        Assert.Equal(2, rb.Count);
        Assert.Equal(rb.Capacity - 1, rb.Head);
        Assert.Equal(1, rb.Tail);
        Assert.Equal(2, rb[0]);
        Assert.Equal(1, rb[1]);
    }

    [Fact]
    public void RemovingElementFromBack()
    {
        var rb = MakeRingBuffer(1, 2);

        var removed = rb.RemoveBack();

        Assert.Single(rb);
        Assert.Equal(0, rb.Head);
        Assert.Equal(1, rb.Tail);
        Assert.Equal(1, rb[0]);
        Assert.Equal(2, removed);
    }

    [Fact]
    public void RemovingElementFromFront()
    {
        var rb = MakeRingBuffer(1, 2);

        var removed = rb.RemoveFront();

        Assert.Single(rb);
        Assert.Equal(1, rb.Head);
        Assert.Equal(2, rb.Tail);
        Assert.Equal(2, rb[0]);
        Assert.Equal(1, removed);
    }

    [Fact]
    public void RemovingElementFromBackEmpty()
    {
        var rb = new RingBuffer<int>();

        Assert.Throws<InvalidOperationException>(() => rb.RemoveBack());
    }

    [Fact]
    public void RemovingElementFromFrontEmpty()
    {
        var rb = new RingBuffer<int>();

        Assert.Throws<InvalidOperationException>(() => rb.RemoveFront());
    }

    [Fact]
    public void ChangingContentsWithIndexer()
    {
        var rb = MakeRingBuffer(1, 2, 3);

        rb[2] = 4;

        Assert.Equal(3, rb.Count);
        Assert.Equal(0, rb.Head);
        Assert.Equal(3, rb.Tail);
        Assert.Equal(1, rb[0]);
        Assert.Equal(2, rb[1]);
        Assert.Equal(4, rb[2]);
    }
}
