using System;
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

    /// <summary>
    /// An optional entry point specified for the script.
    /// </summary>
    public string? EntryPoint { get; }

    /// <summary>
    /// The assembly produced by the script.
    /// Might be null, if the script failed to compile.
    /// </summary>
    public Assembly? Assembly => this.GetAssembly();

    /// <summary>
    /// The errors produced during compilation.
    /// </summary>
    public ImmutableArray<Diagnostic> Errors
    {
        get
        {
            // Force compilation to get the errors
            _ = this.GetAssembly();
            return this.errors;
        }
    }

    // The cached entry point method
    private MethodInfo? entryPoint;
    // NOTE: Extra flag, as the entry point might be null even if it was looked up
    private bool entryPointLookedUp;
    // The cached assembly
    private Assembly? assembly;
    // Cached errors, if the script failed to compile
    private ImmutableArray<Diagnostic> errors;

    internal Script(Compilation compilation, string? entryPoint = null)
    {
        this.Compilation = compilation;
        this.EntryPoint = entryPoint;
    }

    /// <summary>
    /// Executes the script.
    /// </summary>
    /// <returns>The result of the execution.</returns>
    public ExecutionResult<TResult> Execute()
    {
        var assembly = this.GetAssembly();
        if (assembly is null) return ExecutionResult.Fail<TResult>(this.errors);

        var entryPoint = this.GetEntryPoint();
        if (entryPoint is null)
        {
            // This is entirely possible, if the script only contained
            // declarations and no executable code
            return ExecutionResult.Success<TResult>(result: default!);
        }

        // Execute the main method
        var result = (TResult?)entryPoint.Invoke(null, null);
        return ExecutionResult.Success(result!);
    }

    private Assembly? GetAssembly()
    {
        // Check if the script has already failed to compile
        // If it has, don't bother trying to compile it again
        if (!this.errors.IsDefaultOrEmpty) return null;

        // Check, if the assembly is already loaded
        if (this.assembly is not null) return this.assembly;

        // Compile it
        using var peStream = new MemoryStream();
        var emitResult = this.Compilation.Emit(peStream: peStream);

        // Check emission results
        if (!emitResult.Success)
        {
            this.errors = emitResult.Diagnostics;
            return null;
        }

        // Load emitted bytes as assembly
        peStream.Position = 0;
        var peBytes = peStream.ToArray();

        // Cache the assembly
        this.assembly = Assembly.Load(peBytes);
        return this.assembly;
    }

    private MethodInfo? GetEntryPoint()
    {
        // If the entry point was already looked up, return it
        if (this.entryPointLookedUp) return this.entryPoint;

        // We need to look it up, so set the flag
        this.entryPointLookedUp = true;

        // Get the assembly
        var assembly = this.GetAssembly();
        if (assembly is null) return null;

        this.entryPoint = this.EntryPoint is null
            // No explicit entry point, use the assembly's entry point
            ? assembly.EntryPoint
            // Explicit entry point, look it up
            : assembly.GetType(this.Compilation.RootModulePath)?.GetMethod(this.EntryPoint);

        return this.entryPoint;
    }
}
