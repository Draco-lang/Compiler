using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Adapter.Breakpoints;
using Draco.Dap.Adapter;
using Draco.Dap.Model;

namespace Draco.DebugAdapter;

internal sealed partial class DracoDebugAdapter : IExceptionBreakpoints
{
    public IList<ExceptionBreakpointsFilter> ExceptionBreakpointsFilters => Array.Empty<ExceptionBreakpointsFilter>();

    public Task<SetExceptionBreakpointsResponse> SetExceptionBreakpointsAsync(SetExceptionBreakpointsArguments args) =>
        Task.FromResult(new SetExceptionBreakpointsResponse());
}
