using System;
using System.Threading.Tasks;
using Draco.Dap.Attributes;
using Draco.Dap.Model;

namespace Draco.Dap.Adapter;

/// <summary>
/// The interface that debug adapters need to implement.
/// </summary>
public interface IDebugAdapter : IDisposable
{
    // NOTE: This is handled by the lifecycle manager, so it's not annotated
    // The lifecycle manager will dynamically register capabilities here,
    // then invokes these methods
    public Task InitializeAsync(InitializeRequestArguments args);

    // Launching ///////////////////////////////////////////////////////////////

    [Request("launch", Mutating = true)]
    public Task<LaunchResponse> LaunchAsync(LaunchRequestArguments args);

    [Request("attach", Mutating = true)]
    public Task<AttachResponse> AttachAsync(AttachRequestArguments args);

    // Execution ///////////////////////////////////////////////////////////////

    [Request("continue")]
    public Task<ContinueResponse> ContinueAsync(ContinueArguments args);

    [Request("pause")]
    public Task<PauseResponse> PauseAsync(PauseArguments args);

    [Request("terminate")]
    public Task<TerminateResponse> TerminateAsync(TerminateArguments args);

    [Request("stepIn")]
    public Task<StepInResponse> StepIntoAsync(StepInArguments args);

    [Request("next")]
    public Task<NextResponse> StepOverAsync(NextArguments args);

    [Request("stepOut")]
    public Task<StepOutResponse> StepOutAsync(StepOutArguments args);

    // Breakpoints /////////////////////////////////////////////////////////////

    [Request("setBreakpoints", Mutating = true)]
    public Task<SetBreakpointsResponse> SetBreakpointsAsync(SetBreakpointsArguments args);

    // State ///////////////////////////////////////////////////////////////////

    [Request("threads")]
    public Task<ThreadsResponse> GetThreadsAsync();

    [Request("stackTrace")]
    public Task<StackTraceResponse> GetStackTraceAsync(StackTraceArguments args);

    [Request("scopes")]
    public Task<ScopesResponse> GetScopesAsync(ScopesArguments args);

    [Request("variables")]
    public Task<VariablesResponse> GetVariablesAsync(VariablesArguments args);

    // Source //////////////////////////////////////////////////////////////////

    [Request("source")]
    public Task<SourceResponse> GetSourceAsync(SourceArguments args);
}
