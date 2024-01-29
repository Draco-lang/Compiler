using Xunit;

namespace Draco.Debugger.Tests;

public class DebuggerTests
{

    [Fact]
    public async Task simple_program_terminate()
    {
        var host = DebuggerHost.Create(TextDebuggerHelper.FindDbgShim());
        var (debugger, file) = await host.DebugAsync("""
            func main() {
                var i = 0;
                while(i < 10) {
                    i += 1;
                }
            }
            """);
        debugger.Continue();
        await debugger.Terminated;
    }

    [Fact]
    public async Task single_breakpoint()
    {
        var host = DebuggerHost.Create(TextDebuggerHelper.FindDbgShim());
        var (debugger, file) = await host.DebugAsync("""
            import System.Console;
            func main() {
                WriteLine("A");
                WriteLine("B");
                WriteLine("C");
            }
            """);
        if (!file.TryPlaceBreakpoint(3, out var breakpoint))
        {
            throw new InvalidOperationException("Failed to place breakpoint");
        }
        debugger.Continue();

        await breakpoint.Hit;
        debugger.Continue();

        await debugger.Terminated;
    }

    [Fact]
    public async Task multiple_breakpoint_break()
    {
        var host = DebuggerHost.Create(TextDebuggerHelper.FindDbgShim());
        var (debugger, file) = await host.DebugAsync("""
            import System.IO;
            func main() {
                var i = 0;
                while(i < 10) {
                    i += 1;
                }
            }
            """);

        if (!file.TryPlaceBreakpoint(5, out var breakpoint))
        {
            throw new InvalidOperationException("Failed to place breakpoint");
        }
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
    }

    [Fact]
    public async Task hidden_locals_does_not_throw()
    {
        var host = DebuggerHost.Create(TextDebuggerHelper.FindDbgShim());
        var (debugger, file) = await host.DebugAsync("""
            import System.Console;
            func main() {
                var i = 1;
                WriteLine("\{i.ToString()}a");
                WriteLine("\{i.ToString()}b");
                WriteLine("\{i.ToString()}c");
            }
            """);

        if (!file.TryPlaceBreakpoint(4, out var breakpoint))
        {
            throw new InvalidOperationException("Failed to place breakpoint");
        }
        debugger.Continue();
        await breakpoint.Hit;
        var callstack = debugger.MainThread.CallStack;
        var frame = callstack.Single();
        var haveLocal = frame.Locals.TryGetValue("i", out var value);
        Assert.True(haveLocal);
        Assert.Equal(1, value);
        debugger.Continue();
        await debugger.Terminated;
    }
}
