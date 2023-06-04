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

    public Task<SetBreakpointsResponse> SetBreakpointsAsync(SetBreakpointsArguments args) =>
        Task.FromResult(new SetBreakpointsResponse()
        {
            Breakpoints = Array.Empty<Dap.Model.Breakpoint>(),
        });

    public Task<SetExceptionBreakpointsResponse> SetExceptionBreakpointsAsync(SetExceptionBreakpointsArguments args) =>
        Task.FromResult(new SetExceptionBreakpointsResponse());

    private async Task BreakAt(OnBreakpointEventArgs args)
    {
        this.currentThread = args.Thread;
        // TODO: Currently we assume that this is only the entry point breakpoint
        var range = this.TranslateSourceRange(args.Breakpoint.Range);
        await this.client.UpdateBreakpointAsync(new()
        {
            Reason = BreakpointEvent.BreakpointReason.New,
            Breakpoint = new()
            {
                Verified = true,
                Source = this.TranslateSource(args.Breakpoint.SourceFile),
                Line = range?.Start.Line,
                Column = range?.Start.Column,
                EndLine = range?.End.Line,
                EndColumn = range?.End.Column,
            },
        });
        await this.client.OnStoppedAsync(new()
        {
            Reason = StoppedEvent.StoppedReason.Entry,
            AllThreadsStopped = true,
        });
    }
}
