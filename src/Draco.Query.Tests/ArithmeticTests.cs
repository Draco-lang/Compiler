using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Query.Tasks;

namespace Draco.Query.Tests;

// a   b   c
//  \ / \ /
//   v1  v2
//       |
//       v3
public sealed class ArithmeticTests
{
    private int V1_invocations = 0;
    private int V2_invocations = 0;
    private Dictionary<int, int> V3_invocations = new();

    private async QueryValueTask<int> V1(QueryDatabase db)
    {
        ++this.V1_invocations;

        return await db.GetInput<int>("a") + await db.GetInput<int>("b");
    }

    private async QueryValueTask<int> V2(QueryDatabase db)
    {
        ++this.V2_invocations;

        return await db.GetInput<int>("b") * await db.GetInput<int>("c");
    }

    private async QueryValueTask<int> V3(QueryDatabase db, int mul)
    {
        if (!this.V3_invocations.TryGetValue(mul, out var n)) n = 0;
        ++n;
        this.V3_invocations[mul] = n;

        return await this.V2(db) * mul;
    }

    private async QueryValueTask<int> V4(QueryDatabase db, string v1, int k2) =>
        await db.GetInput<int>(v1) + k2;

    [Fact]
    public async void FullComputationOnce()
    {
        var db = new QueryDatabase();

        db.SetInput("a", 1);
        db.SetInput("b", 2);
        db.SetInput("c", 3);

        Assert.Equal(3, await this.V1(db));
        Assert.Equal(6, await this.V3(db, 1));
        Assert.Equal(1, this.V1_invocations);
        Assert.Equal(1, this.V2_invocations);
        Assert.Equal(1, this.V3_invocations[1]);
        Assert.Equal(6, await this.V2(db));
        Assert.Equal(1, this.V2_invocations);
    }

    [Fact]
    public async void UpdateB()
    {
        var db = new QueryDatabase();

        db.SetInput("a", 1);
        db.SetInput("b", 2);
        db.SetInput("c", 3);

        // Now force-eval everyone
        _ = await this.V1(db);
        _ = await this.V3(db, 1);

        // Update b
        db.SetInput("b", 3);

        // Everything should be computed twice
        Assert.Equal(4, await this.V1(db));
        Assert.Equal(9, await this.V3(db, 1));
        Assert.Equal(2, this.V1_invocations);
        Assert.Equal(2, this.V2_invocations);
        Assert.Equal(2, this.V3_invocations[1]);
        Assert.Equal(9, await this.V2(db));
        Assert.Equal(2, this.V2_invocations);
    }

    [Fact]
    public async void FlipBandCEarlyTerminatesV3()
    {
        var db = new QueryDatabase();

        db.SetInput("a", 1);
        db.SetInput("b", 2);
        db.SetInput("c", 3);

        // Now force-eval everyone
        _ = await this.V1(db);
        _ = await this.V3(db, 1);

        // Swap values of b and c
        db.SetInput("b", 3);
        db.SetInput("c", 2);

        // v1 and v2 should be computed twice, v3 shouldn't be re-computed
        Assert.Equal(4, await this.V1(db));
        Assert.Equal(6, await this.V3(db, 1));
        Assert.Equal(2, this.V1_invocations);
        Assert.Equal(2, this.V2_invocations);
        Assert.Equal(1, this.V3_invocations[1]);
        Assert.Equal(6, await this.V2(db));
    }

    [Fact]
    public async void DifferentKeysAreOrthogonal()
    {
        var db = new QueryDatabase();

        db.SetInput("a", 1);
        db.SetInput("b", 2);
        db.SetInput("c", 3);

        Assert.Equal(3, await this.V1(db));
        Assert.Equal(6, await this.V2(db));
        Assert.Equal(6, await this.V3(db, 1));
        Assert.Equal(12, await this.V3(db, 2));
        Assert.Equal(1, this.V1_invocations);
        Assert.Equal(1, this.V2_invocations);
        Assert.Equal(1, this.V3_invocations[1]);
        Assert.Equal(1, this.V3_invocations[2]);
    }

    [Fact]
    public async void KeyPointingAtDependency()
    {
        var db = new QueryDatabase();

        db.SetInput("a", 1);
        db.SetInput("b", 2);
        db.SetInput("c", 3);

        Assert.Equal(1, await this.V4(db, "a", 0));
        Assert.Equal(3, await this.V4(db, "a", 2));
        Assert.Equal(7, await this.V4(db, "b", 5));
        Assert.Equal(9, await this.V4(db, "b", 7));
        Assert.Equal(8, await this.V4(db, "c", 5));

        db.SetInput("b", 1);

        Assert.Equal(1, await this.V4(db, "a", 0));
        Assert.Equal(3, await this.V4(db, "a", 2));
        Assert.Equal(6, await this.V4(db, "b", 5));
        Assert.Equal(8, await this.V4(db, "b", 7));
        Assert.Equal(8, await this.V4(db, "c", 5));
    }
}
