using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Tests.Tasks;
public sealed class SolverTaskTests
{
    [Fact]
    public void TaskSourceWithResultIsCompleted()
    {
        var tcs = new SolverTaskCompletionSource<int>();
        tcs.SetResult(1);
        Assert.True(tcs.IsCompleted);
    }

    [Fact]
    public void ContinuationIsRanOnResult()
    {
        bool continued = false;
        var tcs = new SolverTaskCompletionSource<int>();
        tcs.GetAwaiter().OnCompleted(() =>
        {
            continued = true;
        });
        tcs.SetResult(1);

        Assert.True(continued);
    }

    [Fact]
    public void ContinuationOfSubTaskIsRan()
    {
        var tcs = new SolverTaskCompletionSource<int>();

        async SolverTask<int> AsyncMethod()
        {
            return await tcs;
        }

        var subTask = AsyncMethod();
        Assert.False(subTask.IsCompleted);
        tcs.SetResult(3712);
        Assert.True(subTask.IsCompleted);
        Assert.Equal(3712, subTask.Result);
    }
}
