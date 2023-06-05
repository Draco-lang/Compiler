using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter.Basic;

public interface IProgramState
{
    [Request("stackTrace")]
    public Task<StackTraceResponse> GetStackTraceAsync(StackTraceArguments args);

    [Request("scopes")]
    public Task<ScopesResponse> GetScopesAsync(ScopesArguments args);

    [Request("variables")]
    public Task<VariablesResponse> GetVariablesAsync(VariablesArguments args);
}
