using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Adapter.Breakpoints;
using Draco.Dap.Adapter;
using Draco.Dap.Model;
using Draco.Debugger;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IExceptionBreakpoints
{
    public IList<ExceptionBreakpointsFilter> ExceptionBreakpointsFilters => Array.Empty<ExceptionBreakpointsFilter>();

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

    public Task<SetExceptionBreakpointsResponse> SetExceptionBreakpointsAsync(SetExceptionBreakpointsArguments args) =>
        Task.FromResult(new SetExceptionBreakpointsResponse());

    private async Task BreakAt(Debugger.Thread thread, StoppedEvent.StoppedReason reason)
    {
        this.currentThread = thread;
        await this.client.OnStoppedAsync(new()
        {
            Reason = reason,
            AllThreadsStopped = true,
            // TODO: Hardcoded
            ThreadId = 0,
        });
    }
}
