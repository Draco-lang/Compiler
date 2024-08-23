using System.IO;
using System.Reflection;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Codegen;

namespace Draco.Compiler.Api.Scripting;

/// <summary>
/// Exposes a scripting API.
/// </summary>
public static class ScriptingEngine
{
    /// <summary>
    /// Executes the code of the given compilation as a full program.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to execute.</param>
    /// <returns>The result of the execution.</returns>
    public static ExecutionResult<object?> ExecuteProgram(Compilation compilation) =>
        ExecuteProgram<object?>(compilation);

    /// <summary>
    /// Executes the code of the given compilation as a full program.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="compilation">The <see cref="Compilation"/> to execute.</param>
    /// <returns>The result of the execution.</returns>
    public static ExecutionResult<TResult> ExecuteProgram<TResult>(Compilation compilation)
    {
        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream: peStream);

        // Check emission results
        if (!emitResult.Success) return ExecutionResult.Fail<TResult>(emitResult.Diagnostics);

        // Load emitted bytes as assembly
        peStream.Position = 0;
        var peBytes = peStream.ToArray();
        var assembly = Assembly.Load(peBytes);

        var mainMethod = assembly.EntryPoint;

        if (mainMethod is null)
        {
            var diag = Diagnostic.Create(
                template: CodegenErrors.NoMainMethod,
                location: Location.None);
            return ExecutionResult.Fail<TResult>([diag]);
        }

        var result = (TResult?)mainMethod.Invoke(null, []);
        return ExecutionResult.Success(result!);
    }
}
