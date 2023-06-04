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
                if (source.TryPlaceBreakpoint(position, out var breakpoint))
                {
                    result.Add(new()
                    {
                        Verified = true,
                        Line = breakpoint.Range?.Start.Line,
                        Column = breakpoint.Range?.Start.Column,
                        EndLine = breakpoint.Range?.End.Line,
                        EndColumn = breakpoint.Range?.End.Column,
                        Source = args.Source,
                    });
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

    private async Task BreakAt(OnBreakpointEventArgs args)
    {
        this.currentThread = args.Thread;
        // TODO: Currently we assume that this is only the entry point breakpoint
        await this.client.OnStoppedAsync(new()
        {
            Reason = StoppedEvent.StoppedReason.Entry,
            AllThreadsStopped = true,
        });
    }
}
