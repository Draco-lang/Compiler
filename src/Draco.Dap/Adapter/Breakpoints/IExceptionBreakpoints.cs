using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Breakpoints;

public interface IExceptionBreakpoints
{
    [Capability(nameof(Capabilities.ExceptionBreakpointFilters))]
    public IList<ExceptionBreakpointsFilter> ExceptionBreakpointsFilters { get; }

    [Request("setExceptionBreakpoints", Mutating = true)]
    public Task<SetExceptionBreakpointsResponse> SetExceptionBreakpointsAsync(SetExceptionBreakpointsArguments args);
}
