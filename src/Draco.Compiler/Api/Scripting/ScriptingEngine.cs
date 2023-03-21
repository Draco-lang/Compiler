using System;
using System.Collections.Immutable;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.Scripting;

/// <summary>
/// The result type of script execution.
/// </summary>
/// <typeparam name="TResult">The expected result type.</typeparam>
/// <param name="Success">True, if the execution was successful without errors.</param>
/// <param name="Result">The result of execution.</param>
/// <param name="Diagnostics">The <see cref="Diagnostic"/>s produced during execution.</param>
public readonly record struct ExecutionResult<TResult>(
    bool Success,
    TResult? Result,
    ImmutableArray<Diagnostic> Diagnostics);

/// <summary>
/// Exposes a scripting API.
/// </summary>
public static class ScriptingEngine
{
    /// <summary>
    /// Executes the code of the given compilation.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to execute.</param>
    /// <returns>The result of the execution.</returns>
    public static ExecutionResult<object?> Execute(
        Compilation compilation) =>
        Execute<object?>(compilation);

    /// <summary>
    /// Executes the code of the given compilation.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="compilation">The <see cref="Compilation"/> to execute.</param>
    /// <returns>The result of the execution.</returns>
    public static ExecutionResult<TResult> Execute<TResult>(
        Compilation compilation)
    {
        throw new NotImplementedException();
    }
}
