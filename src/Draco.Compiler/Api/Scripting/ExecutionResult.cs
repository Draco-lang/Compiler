using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.Scripting;

/// <summary>
/// Utility factory methods for creating <see cref="ExecutionResult{TResult}"/> instances.
/// </summary>
internal static class ExecutionResult
{
    public static ExecutionResult<TResult> Fail<TResult>(ImmutableArray<Diagnostic> diagnostics) =>
        new(Success: false, Value: default, Diagnostics: diagnostics);

    public static ExecutionResult<TResult> Success<TResult>(
        TResult result, ImmutableArray<Diagnostic>? diagnostics = null) =>
        new(Success: true, Value: result, Diagnostics: diagnostics ?? []);
}

/// <summary>
/// The result type of script execution.
/// </summary>
/// <typeparam name="TResult">The expected result type.</typeparam>
/// <param name="Success">True, if the execution was successful without errors.</param>
/// <param name="Value">The resulting value of the execution.</param>
/// <param name="Diagnostics">The <see cref="Diagnostic"/>s produced during execution.</param>
public readonly record struct ExecutionResult<TResult>(
    bool Success,
    TResult? Value,
    ImmutableArray<Diagnostic> Diagnostics);
