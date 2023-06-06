using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Dap.Adapter;
using Draco.Dap.Adapter.Breakpoints;
using Draco.Dap.Model;
using Draco.Debugger;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IDebugAdapter
{
    public IList<ExceptionBreakpointsFilter> ExceptionBreakpointsFilters => Array.Empty<ExceptionBreakpointsFilter>();

    private readonly IDebugClient client;

    private InitializeRequestArguments clientInfo = null!;
    private Translator translator = null!;
    private DebuggerHost debuggerHost = null!;
    private Debugger.Debugger debugger = null!;

    private LaunchRequestArguments? launchArgs;
    private AttachRequestArguments? attachArgs;

    public DracoDebugAdapter(IDebugClient client)
    {
        this.client = client;
    }

    public void Dispose() { }

    public Task InitializeAsync(InitializeRequestArguments args)
    {
        this.clientInfo = args;
        this.translator = new(args);

        var dbgShim = FindDbgShim();
        this.debuggerHost = DebuggerHost.Create(dbgShim);

        return Task.CompletedTask;
    }

    // TODO: Temporary
    private static string FindDbgShim()
    {
        var root = "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App";

        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"Cannot find dbgshim.dll: '{root}' does not exist");
        }

        foreach (var dir in Directory.EnumerateDirectories(root).Reverse())
        {
            var dbgshim = Directory.EnumerateFiles(dir, "dbgshim.dll").FirstOrDefault();
            if (dbgshim is not null) return dbgshim;
        }

        throw new InvalidOperationException($"Failed to find a runtime containing dbgshim.dll under '{root}'");
    }

    // Launching ///////////////////////////////////////////////////////////////

    public Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args)
    {
        this.launchArgs = args;

        var toRun = args.LaunchAttributes!["program"].GetString()!;
        // TODO: Consider no-debug
        this.debugger = this.debuggerHost.StartProcess("dotnet", toRun);

        this.debugger.OnStandardOut += async (_, args) => await this.client.SendOutputAsync(new()
        {
            Category = OutputEvent.OutputCategory.Stdout,
            Output = args,
        });
        this.debugger.OnExited += async (_, e) => await this.OnDebuggerExited(e);

        this.debugger.OnBreakpoint += async (_, a) =>
        {
            await this.BreakAt(
                a.Thread,
                a.Breakpoint.IsEntryPoint
                    ? StoppedEvent.StoppedReason.Entry
                    : StoppedEvent.StoppedReason.Breakpoint);
        };
        this.debugger.OnStep += async (_, a) =>
        {
            if (a.Range is null)
            {
                a.Thread?.StepOver();
                return;
            }
            await this.BreakAt(a.Thread, StoppedEvent.StoppedReason.Pause);
        };

        return Task.FromResult(new LaunchResponse());
    }

    public Task<AttachResponse> AttachAsync(AttachRequestArguments args)
    {
        this.attachArgs = args;
        throw new NotSupportedException("attaching is not yet supported");
    }

    private async Task OnDebuggerExited(int exitCode)
    {
        await this.client.DebuggerTerminatedAsync(new());
        await this.client.ProcessExitedAsync(new()
        {
            ExitCode = exitCode,
        });
    }

    // Execution ///////////////////////////////////////////////////////////////

    public Task<ContinueResponse> ContinueAsync(ContinueArguments args)
    {
        if (args.SingleThread == true)
        {
            // TODO
            // var thread = this.debugger.Threads.FirstOrDefault(t => t.Id == args.ThreadId);
            // thread?.Continue();
            throw new NotSupportedException("continuing a single thread is not yet supported");
        }
        else
        {
            this.debugger.Continue();
        }
        return Task.FromResult(new ContinueResponse());
    }

    public Task<PauseResponse> PauseAsync(PauseArguments args)
    {
        // TODO
        throw new NotSupportedException("pausing is not yet supported");
    }

    public Task<TerminateResponse> TerminateAsync(TerminateArguments args)
    {
        // TODO
        throw new NotSupportedException("terminating is not yet supported");
    }

    public Task<StepInResponse> StepIntoAsync(StepInArguments args)
    {
        var thread = this.debugger.Threads.FirstOrDefault(t => t.Id == args.ThreadId);
        thread?.StepInto();
        return Task.FromResult(new StepInResponse());
    }

    public Task<NextResponse> StepOverAsync(NextArguments args)
    {
        var thread = this.debugger.Threads.FirstOrDefault(t => t.Id == args.ThreadId);
        thread?.StepOver();
        return Task.FromResult(new NextResponse());
    }

    public Task<StepOutResponse> StepOutAsync(StepOutArguments args)
    {
        var thread = this.debugger.Threads.FirstOrDefault(t => t.Id == args.ThreadId);
        thread?.StepOut();
        return Task.FromResult(new StepOutResponse());
    }

    // Breakpoints /////////////////////////////////////////////////////////////

    public Task<SetBreakpointsResponse> SetBreakpointsAsync(SetBreakpointsArguments args)
    {
        var result = new List<Dap.Model.Breakpoint>();
        var source = this.debugger.MainModule.SourceFiles
            .FirstOrDefault(s => PathEqualityComparer.Instance.Equals(s.Uri.AbsolutePath, args.Source.Path));
        if (args.Breakpoints is not null && source is not null)
        {
            foreach (var bp in args.Breakpoints)
            {
                var position = this.translator.ToDebugger(bp.Line, bp.Column ?? 0);
                var success = bp.Column is null
                    ? source.TryPlaceBreakpoint(bp.Line, out var breakpoint)
                    : source.TryPlaceBreakpoint(this.translator.ToDebugger(bp.Line, bp.Column.Value), out breakpoint);
                if (success)
                {
                    result.Add(this.translator.ToDap(breakpoint!));
                }
                else
                {
                    result.Add(new() { Verified = false });
                }
            }
        }
        return Task.FromResult(new SetBreakpointsResponse()
        {
            Breakpoints = result,
        });
    }

    private Task BreakAt(Debugger.Thread thread, StoppedEvent.StoppedReason reason) =>
        this.client.OnStoppedAsync(new()
        {
            Reason = reason,
            AllThreadsStopped = true,
            ThreadId = thread.Id,
        });

    // State ///////////////////////////////////////////////////////////////////

    public Task<ThreadsResponse> GetThreadsAsync() =>
        Task.FromResult(new ThreadsResponse()
        {
            Threads = this.debugger.Threads
                .Select(this.translator.ToDap)
                .ToList(),
        });

    public Task<StackTraceResponse> GetStackTraceAsync(StackTraceArguments args)
    {
        this.translator.ClearCache();
        var thread = this.debugger.Threads.FirstOrDefault(t => t.Id == args.ThreadId);
        var result = thread is null
            ? Array.Empty<Dap.Model.StackFrame>()
            : thread.CallStack.Select(this.translator.ToDap).ToArray();
        return Task.FromResult(new StackTraceResponse()
        {
            StackFrames = result,
            TotalFrames = result.Length,
        });
    }

    public Task<ScopesResponse> GetScopesAsync(ScopesArguments args)
    {
        var frame = this.translator.GetStackFrameById(args.FrameId);
        if (frame is null)
        {
            return Task.FromResult(new ScopesResponse()
            {
                Scopes = Array.Empty<Scope>(),
            });
        }

        // We build up exactly two scopes, arguments and locals
        var argsId = this.translator.CacheValue(frame.Arguments);
        var argumentsScope = new Scope()
        {
            Expensive = false,
            Name = "Arguments",
            VariablesReference = argsId,
        };
        var localsId = this.translator.CacheValue(frame.Locals);
        var localsScope = new Scope()
        {
            Expensive = false,
            Name = "Locals",
            VariablesReference = localsId,
        };
        return Task.FromResult(new ScopesResponse()
        {
            Scopes = new[] { argumentsScope, localsScope },
        });
    }

    public Task<VariablesResponse> GetVariablesAsync(VariablesArguments args)
    {
        var variables = this.translator.GetVariables(args.VariablesReference);
        return Task.FromResult(new VariablesResponse()
        {
            Variables = variables,
        });
    }

    // Source //////////////////////////////////////////////////////////////////

    public Task<SourceResponse> GetSourceAsync(SourceArguments args) =>
        throw new NotSupportedException("providing additional sources is not yet supported");
}
