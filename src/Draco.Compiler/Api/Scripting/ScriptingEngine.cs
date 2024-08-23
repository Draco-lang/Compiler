using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Scripting;

/// <summary>
/// Exposes a scripting API.
/// </summary>
public static class ScriptingEngine
{
    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<object?> CreateScript(
        string code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null) =>
        CreateScript(code.AsMemory(), globalImports, metadataReferences, previousCompilation);

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<object?> CreateScript(
        ReadOnlyMemory<char> code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null) =>
        CreateScript<object?>(code, globalImports, metadataReferences, previousCompilation);

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<TResult> CreateScript<TResult>(
        string code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null) =>
        CreateScript<TResult>(code.AsMemory(), globalImports, metadataReferences, previousCompilation);

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<TResult> CreateScript<TResult>(
        ReadOnlyMemory<char> code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null)
    {
        var moduleName = $"Context_{Guid.NewGuid():N}";
        var syntaxTree = SyntaxTree.ParseScript(SourceReader.From(code));
        var compilation = Compilation.Create(
            syntaxTrees: [syntaxTree],
            flags: CompilationFlags.ScriptingMode | CompilationFlags.ImplicitPublicSymbols,
            globalImports: globalImports,
            metadataReferences: metadataReferences,
            rootModulePath: moduleName,
            assemblyName: moduleName,
            metadataAssemblies: previousCompilation?.MetadataAssembliesDict);
        return new Script<TResult>(compilation);
    }

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
