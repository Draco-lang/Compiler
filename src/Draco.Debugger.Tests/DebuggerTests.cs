using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Draco.Debugger.Tests;

public sealed class DebuggerTests
{
    private readonly ITestOutputHelper output;

    public DebuggerTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static async Task Timeout(Func<Task> action, int timeoutSecs = 10)
    {
        var task = action();
        await task.WaitAsync(TimeSpan.FromSeconds(timeoutSecs));
        if (!task.IsCompleted)
        {
            throw new TimeoutException();
        }
        await task; // await the task to throw any exceptions it might have thrown.
    }

    [SkippableFact]
    public async Task SimpleProgramTerminate() => await Timeout(async () =>
    {
        var session = await TestDebugSession.DebugAsync("""
            func main() {
                var i = 0;
                while(i < 10) {
                    i += 1;
                }
            }
            """,
        this.output);
        var debugger = session.Debugger;
        debugger.Continue();
        await debugger.Terminated;
    });

    [SkippableFact]
    public async Task SingleBreakpoint() => await Timeout(async () =>
    {
        var session = await TestDebugSession.DebugAsync("""
import System.Console;
func main() {
    WriteLine("A");
    WriteLine("B");
    WriteLine("C");
}
""",
                                        this.output);
        var debugger = session.Debugger;
        Assert.True(session.File.TryPlaceBreakpoint(3, out var breakpoint));
        debugger.Continue();

        await breakpoint.Hit;
        debugger.Continue();

        await debugger.Terminated;
    });

    [SkippableFact]
    public async Task MultipleBreakpointBreak() => await Timeout(async () =>
    {
        var session = await TestDebugSession.DebugAsync("""
                import System.IO;
                func main() {
                    var i = 0;
                    while(i < 10) {
                        i += 1;
                    }
                }
                """,
            this.output);

        Assert.True(session.File.TryPlaceBreakpoint(5, out var breakpoint));

        var debugger = session.Debugger;
        debugger.Continue();
        for (var i = 0; i < 10; i++)
        {
            await breakpoint.Hit;
            var callstack = debugger.MainThread.CallStack;
            var frame = callstack.Single();
            var haveLocal = frame.Locals.TryGetValue("i", out var value);
            Assert.True(haveLocal);
            Assert.Equal(i, value);
            debugger.Continue();
        }
        await debugger.Terminated;
    });

    [SkippableFact]
    public async Task HiddenLocalsDoesNotThrow() => await Timeout(async () =>
    {
        var session = await TestDebugSession.DebugAsync("""
                import System.Console;
                func main() {
                    var i = 1;
                    WriteLine("\{i.ToString()}a");
                    WriteLine("\{i.ToString()}b");
                    WriteLine("\{i.ToString()}c");
                }
                """,
            this.output);

        var debugger = session.Debugger;

        Assert.True(session.File.TryPlaceBreakpoint(4, out var breakpoint));
        debugger.Continue();
        await breakpoint.Hit;
        var callstack = debugger.MainThread.CallStack;
        var frame = callstack.Single();
        var haveLocal = frame.Locals.TryGetValue("i", out var value);
        Assert.True(haveLocal);
        Assert.Equal(1, value);
        debugger.Continue();
        await debugger.Terminated;
    });
}
