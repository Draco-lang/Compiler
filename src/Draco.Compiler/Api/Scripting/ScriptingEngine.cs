using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;

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
    public static ExecutionResult<object?> Execute(Compilation compilation) => Execute<object?>(compilation);

    /// <summary>
    /// Executes the code of the given compilation.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="compilation">The <see cref="Compilation"/> to execute.</param>
    /// <returns>The result of the execution.</returns>
    public static ExecutionResult<TResult> Execute<TResult>(Compilation compilation)
    {
        // First check up to semantic errors
        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Length > 0)
        {
            return new(
                Success: false,
                Result: default,
                Diagnostics: diagnostics);
        }

        // Try to compile
        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(
            peStream,
            csCompilerOptionBuilder: opt => new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        // Check emission results
        if (!emitResult.Success)
        {
            return new(
                Success: false,
                Result: default,
                Diagnostics: emitResult.Diagnostics);
        }

        // Load emitted bytes as assembly
        peStream.Position = 0;
        var peBytes = peStream.ToArray();
        var assembly = Assembly.Load(peBytes);

        var mainMethod = assembly
            .GetType("Program")?
            .GetMethod("Main");

        if (mainMethod is null)
        {
            var diag = Diagnostic.Create(
                template: CodegenErrors.NoMainMethod,
                location: Location.None);
            return new(
                Success: false,
                Result: default,
                Diagnostics: ImmutableArray.Create(diag));
        }

        var result = (TResult?)mainMethod.Invoke(null, new[] { Array.Empty<string>() });
        return new(
            Success: true,
            Result: result,
            Diagnostics: ImmutableArray<Diagnostic>.Empty);
    }
}
