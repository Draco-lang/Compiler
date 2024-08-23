using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.Scripting;

/// <summary>
/// Represents a script to be executed.
/// </summary>
/// <typeparam name="TResult">The execution result type.</typeparam>
public sealed class Script<TResult>
{
    /// <summary>
    /// The compilation of the script.
    /// </summary>
    public Compilation Compilation { get; }

    // The cached assembly
    private Assembly? assembly;
    // Cached errors, if the script failed to compile
    private ImmutableArray<Diagnostic> errors;

    internal Script(Compilation compilation)
    {
        this.Compilation = compilation;
    }

    /// <summary>
    /// Executes the script.
    /// </summary>
    /// <returns>The result of the execution.</returns>
    public ExecutionResult<TResult> Execute()
    {
        // Check if the assembly is already loaded
        if (this.assembly is null)
        {
            // Check if the script has already failed to compile
            // If it has, don't bother trying to compile it again
            if (!this.errors.IsDefaultOrEmpty) return ExecutionResult.Fail<TResult>(this.errors);

            using var peStream = new MemoryStream();
            var emitResult = this.Compilation.Emit(peStream: peStream);

            // Check emission results
            if (!emitResult.Success)
            {
                this.errors = emitResult.Diagnostics;
                return ExecutionResult.Fail<TResult>(emitResult.Diagnostics);
            }

            // Load emitted bytes as assembly
            peStream.Position = 0;
            var peBytes = peStream.ToArray();

            // Cache the assembly
            this.assembly = Assembly.Load(peBytes);
        }

        var mainMethod = this.assembly.EntryPoint;
        if (mainMethod is null)
        {
            // This is entirely possible, if the script only contained
            // declarations and no executable code.
            return ExecutionResult.Success<TResult>(result: default!);
        }

        // Execute the main method
        var result = (TResult?)mainMethod.Invoke(null, null);
        return ExecutionResult.Success(result!);
    }
}
