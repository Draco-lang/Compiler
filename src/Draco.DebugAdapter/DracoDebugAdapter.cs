using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Draco.Dap.Adapter;
using Draco.Dap.Model;
using Draco.Debugger;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IDebugAdapter
{
    private readonly IDebugClient client;

    private InitializeRequestArguments clientInfo = null!;
    private Translator translator = null!;
    private DebuggerHost debuggerHost = null!;

    private Debugger.Debugger? debugger;

    private LaunchRequestArguments? launchArgs;
    private AttachRequestArguments? attachArgs;

    private readonly Queue<SetBreakpointsArguments> breakpointRequestQueue = new();

    public DracoDebugAdapter(IDebugClient client)
    {
        this.client = client;
    }

    public void Dispose() { }

    public async Task InitializeAsync(InitializeRequestArguments args)
    {
        this.clientInfo = args;
        this.translator = new(args);

        this.debuggerHost = DebuggerHost.Create();

        // Starts the configuration sequence
        await this.client.Initialized();
    }

    // Launching ///////////////////////////////////////////////////////////////

    public async Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args)
    {
        this.launchArgs = args;

        var toRun = args.LaunchAttributes!["program"].GetString()!;
        // TODO: Consider no-debug
        this.debugger = this.debuggerHost.StartProcess("dotnet", toRun);

        this.debugger.OnEventLog += async (_, e) => await this.client.SendOutputAsync(new()
        {
            Category = OutputEvent.OutputCategory.Console,
            Output = e,
        });

        this.debugger.OnStandardOut += async (_, args) => await this.client.SendOutputAsync(new()
        {
            Category = OutputEvent.OutputCategory.Stdout,
            Output = args,
        });
        this.debugger.OnStandardError += async (_, args) => await this.client.SendOutputAsync(new()
        {
            Category = OutputEvent.OutputCategory.Stderr,
            Output = args,
        });
        this.debugger.OnExited += async (_, e) => await this.OnDebuggerExited(e);

        this.debugger.OnBreakpoint += async (_, a) => await this.OnBreakpoint(a);
        this.debugger.OnStep += async (_, a) =>
        {
            if (a.Range is null)
            {
                a.Thread?.StepOver();
                return;
            }
            await this.BreakAt(a.Thread, StoppedEvent.StoppedReason.Step);
        };
        this.debugger.OnPause += async (_, a) =>
        {
            var thread = this.debugger.MainThread;
            await this.BreakAt(thread, StoppedEvent.StoppedReason.Pause);
        };

        await this.client.ProcessStartedAsync(new()
        {
            Name = toRun,
        });

        return new LaunchResponse();
    }

    private async Task OnBreakpoint(OnBreakpointEventArgs args)
    {
        var reason = args.Breakpoint.IsEntryPoint
            ? StoppedEvent.StoppedReason.Entry
            : StoppedEvent.StoppedReason.Breakpoint;
        if (reason == StoppedEvent.StoppedReason.Entry)
        {
            // Any stashed breakpoints should be set
            while (this.breakpointRequestQueue.TryDequeue(out var req) && req.Breakpoints is not null)
            {
                var result = this.SetBreakpointsImpl(req);
                // Send updates about the breakpoints
                foreach (var (reqBp, bp) in req.Breakpoints.Zip(result))
                {
                    await this.client.UpdateBreakpointAsync(new()
                    {
                        Reason = BreakpointEvent.BreakpointReason.Changed,
                        Breakpoint = bp,
                    });
                }
            }

            var shouldStopAtEntry = this.launchArgs?.LaunchAttributes!["stopAtEntry"].GetBoolean() ?? false;
            if (shouldStopAtEntry)
            {
                await this.BreakAt(args.Thread, reason);
            }
            else
            {
                // We should not stop at the entry point
                this.debugger?.Continue();
            }
        }
        else
        {
            await this.BreakAt(args.Thread, reason);
        }
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

    private Debugger.Thread? GetThreadById(int id) =>
        this.debugger?.Threads.FirstOrDefault(t => t.Id == id);

    public Task<ContinueResponse> ContinueAsync(ContinueArguments args)
    {
        if (args.SingleThread == true)
        {
            // TODO
            // var thread = this.GetThreadById(args.ThreadId);
            // thread?.Continue();
            throw new NotSupportedException("continuing a single thread is not yet supported");
        }
        else
        {
            this.debugger?.Continue();
        }
        return Task.FromResult(new ContinueResponse());
    }

    public Task<PauseResponse> PauseAsync(PauseArguments args)
    {
        this.debugger?.Pause();
        return Task.FromResult(new PauseResponse());
    }

    public Task<TerminateResponse> TerminateAsync(TerminateArguments args)
    {
        // TODO
        throw new NotSupportedException("terminating is not yet supported");
    }

    public Task<StepInResponse> StepIntoAsync(StepInArguments args)
    {
        var thread = this.GetThreadById(args.ThreadId);
        thread?.StepInto();
        return Task.FromResult(new StepInResponse());
    }

    public Task<NextResponse> StepOverAsync(NextArguments args)
    {
        var thread = this.GetThreadById(args.ThreadId);
        thread?.StepOver();
        return Task.FromResult(new NextResponse());
    }

    public Task<StepOutResponse> StepOutAsync(StepOutArguments args)
    {
        var thread = this.GetThreadById(args.ThreadId);
        thread?.StepOut();
        return Task.FromResult(new StepOutResponse());
    }

    // Breakpoints /////////////////////////////////////////////////////////////

    public Task<SetBreakpointsResponse> SetBreakpointsAsync(SetBreakpointsArguments args)
    {
        if (this.debugger is null)
        {
            // Not running yet, stash
            this.breakpointRequestQueue.Enqueue(args);
            return Task.FromResult(new SetBreakpointsResponse()
            {
                Breakpoints = args.Breakpoints?
                    .Select(bp => new Dap.Model.Breakpoint()
                    {
                        Verified = false,
                        Id = this.translator.AllocateId(bp),
                    })
                    .ToArray() ?? Array.Empty<Dap.Model.Breakpoint>(),
            });
        }

        // Running, we can set it
        return Task.FromResult(new SetBreakpointsResponse()
        {
            Breakpoints = this.SetBreakpointsImpl(args),
        });
    }

    private IList<Dap.Model.Breakpoint> SetBreakpointsImpl(SetBreakpointsArguments args)
    {
        if (this.debugger is null) throw new InvalidOperationException("cannot set up breakpoints without a running debugger");

        var result = new List<Dap.Model.Breakpoint>();
        var source = this.debugger.MainModule.SourceFiles
            .FirstOrDefault(s => PathEqualityComparer.Instance.Equals(s.Uri.AbsolutePath, args.Source.Path));
        if (args.Breakpoints is not null && source is not null)
        {
            // Remove old breakpoints
            foreach (var bp in source.Breakpoints) bp.Remove();

            // Add new breakpoints
            foreach (var bp in args.Breakpoints)
            {
                var position = this.translator.ToDebugger(bp.Line, bp.Column ?? 0);
                var success = bp.Column is null
                    ? source.TryPlaceBreakpoint(bp.Line, out var breakpoint)
                    : source.TryPlaceBreakpoint(this.translator.ToDebugger(bp.Line, bp.Column.Value), out breakpoint);
                if (success)
                {
                    result.Add(this.translator.ToDap(breakpoint!, id: this.translator.AllocateId(bp)));
                }
                else
                {
                    result.Add(new()
                    {
                        Verified = false,
                        Id = this.translator.AllocateId(bp),
                    });
                }
            }
        }
        return result;
    }

    private Task BreakAt(Debugger.Thread? thread, StoppedEvent.StoppedReason reason) =>
        this.client.StoppedAsync(new()
        {
            Reason = reason,
            AllThreadsStopped = true,
            ThreadId = thread?.Id,
        });

    // State ///////////////////////////////////////////////////////////////////

    public Task<ThreadsResponse> GetThreadsAsync() =>
        Task.FromResult(new ThreadsResponse()
        {
            Threads = this.debugger?.Threads
                .Select(this.translator.ToDap)
                .ToArray() ?? Array.Empty<Dap.Model.Thread>(),
        });

    public Task<StackTraceResponse> GetStackTraceAsync(StackTraceArguments args)
    {
        this.translator.ClearCache();
        var thread = this.GetThreadById(args.ThreadId);
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
            PresentationHint = Scope.ScopePresentationHint.Arguments,
            VariablesReference = argsId,
        };
        var localsId = this.translator.CacheValue(frame.Locals);
        var localsScope = new Scope()
        {
            Expensive = false,
            Name = "Locals",
            PresentationHint = Scope.ScopePresentationHint.Locals,
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
