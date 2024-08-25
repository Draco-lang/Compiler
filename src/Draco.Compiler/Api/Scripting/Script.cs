using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.Symbols.Script;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Scripting;

/// <summary>
/// Exposes a scripting API.
/// </summary>
public static class Script
{
    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <param name="assemblyLoadContext">Optional assembly load context to use for loading the script assembly.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<object?> Create(
        string code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null,
        AssemblyLoadContext? assemblyLoadContext = null) =>
        Create(code.AsMemory(), globalImports, metadataReferences, previousCompilation, assemblyLoadContext);

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <param name="assemblyLoadContext">Optional assembly load context to use for loading the script assembly.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<object?> Create(
        ReadOnlyMemory<char> code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null,
        AssemblyLoadContext? assemblyLoadContext = null) =>
        Create<object?>(code, globalImports, metadataReferences, previousCompilation, assemblyLoadContext);

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <param name="assemblyLoadContext">Optional assembly load context to use for loading the script assembly.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<TResult> Create<TResult>(
        string code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null,
        AssemblyLoadContext? assemblyLoadContext = null) =>
        Create<TResult>(code.AsMemory(), globalImports, metadataReferences, previousCompilation, assemblyLoadContext);

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="code">The code of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <param name="assemblyLoadContext">Optional assembly load context to use for loading the script assembly.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<TResult> Create<TResult>(
        ReadOnlyMemory<char> code,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null,
        AssemblyLoadContext? assemblyLoadContext = null)
    {
        var syntaxTree = SyntaxTree.ParseScript(SourceReader.From(code));
        return Create<TResult>(syntaxTree, globalImports, metadataReferences, previousCompilation, assemblyLoadContext);
    }

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <param name="assemblyLoadContext">Optional assembly load context to use for loading the script assembly.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<object?> Create(
        SyntaxTree syntaxTree,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null,
        AssemblyLoadContext? assemblyLoadContext = null) =>
        Create<object?>(syntaxTree, globalImports, metadataReferences, previousCompilation, assemblyLoadContext);

    /// <summary>
    /// Creates a new script from the given code.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="syntaxTree">The syntax tree of the script.</param>
    /// <param name="globalImports">Optional global imports to use in the script.</param>
    /// <param name="metadataReferences">Optional metadata references to use in the script.</param>
    /// <param name="previousCompilation">Optional previous compilation to use for incremental compilation.</param>
    /// <param name="assemblyLoadContext">Optional assembly load context to use for loading the script assembly.</param>
    /// <returns>The created <see cref="Script{TResult}"/>.</returns>
    public static Script<TResult> Create<TResult>(
        SyntaxTree syntaxTree,
        GlobalImports globalImports = default,
        ImmutableArray<MetadataReference>? metadataReferences = null,
        Compilation? previousCompilation = null,
        AssemblyLoadContext? assemblyLoadContext = null)
    {
        var moduleName = $"Context_{Guid.NewGuid():N}";
        var compilation = Compilation.Create(
            syntaxTrees: [syntaxTree],
            flags: CompilationFlags.ScriptingMode,
            globalImports: globalImports,
            metadataReferences: metadataReferences,
            rootModulePath: moduleName,
            assemblyName: moduleName,
            metadataAssemblies: previousCompilation?.MetadataAssembliesDict);
        return new Script<TResult>(
            compilation: compilation,
            assemblyLoadContext: assemblyLoadContext);
    }

    /// <summary>
    /// Executes the code of the given compilation as a full program.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to execute.</param>
    /// <returns>The result of the execution.</returns>
    public static ExecutionResult<object?> ExecuteAsProgram(Compilation compilation) =>
        ExecuteAsProgram<object?>(compilation);

    /// <summary>
    /// Executes the code of the given compilation as a full program.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <param name="compilation">The <see cref="Compilation"/> to execute.</param>
    /// <returns>The result of the execution.</returns>
    public static ExecutionResult<TResult> ExecuteAsProgram<TResult>(Compilation compilation)
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

/// <summary>
/// Represents a script to be executed.
/// </summary>
/// <typeparam name="TResult">The execution result type.</typeparam>
public sealed class Script<TResult>
{
    /// <summary>
    /// The assembly load context of the script.
    /// </summary>
    public AssemblyLoadContext AssemblyLoadContext { get; }

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

    /// <summary>
    /// The entry point method of the script.
    /// Might be null, if the script failed to compile or no entry point was found.
    /// </summary>
    public MethodInfo? EntryPointMethod => this.GetEntryPoint();

    /// <summary>
    /// The globally exported symbols that can be used for importing for future contexts.
    /// </summary>
    public GlobalImports GlobalImports
    {
        get
        {
            var rootModule = (ScriptModuleSymbol)this.Compilation.SourceModule;
            return rootModule.GlobalImports;
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

    internal Script(
        Compilation compilation,
        string? entryPoint = null,
        AssemblyLoadContext? assemblyLoadContext = null)
    {
        if (!compilation.Flags.HasFlag(CompilationFlags.ScriptingMode))
        {
            throw new InvalidOperationException("the compilation is not in scripting mode");
        }

        this.AssemblyLoadContext = assemblyLoadContext ?? AssemblyLoadContext.Default;
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

        // Load emitted bytes as assembly and cache
        peStream.Position = 0;
        this.assembly = this.AssemblyLoadContext.LoadFromStream(peStream);

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
