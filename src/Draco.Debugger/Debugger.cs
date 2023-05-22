using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

/// <summary>
/// Represents a debugger for a single process.
/// </summary>
public sealed class Debugger
{
    /// <summary>
    /// The path to the started program.
    /// </summary>
    public string ProgramPath { get; }

    /// <summary>
    /// The assumed PDB path of the started program.
    /// </summary>
    public string PdbPath => Path.ChangeExtension(this.ProgramPath, ".pdb");

    /// <summary>
    /// The task that is completed, when the process has terminated.
    /// </summary>
    public Task Terminated => this.terminatedCompletionSource.Task;

    /// <summary>
    /// The source files discovered in the PDB.
    /// </summary>
    public ImmutableDictionary<Uri, SourceFile> SourceFiles => this.sourceFiles ??= this.BuildSourceFiles();
    private ImmutableDictionary<Uri, SourceFile>? sourceFiles;

    /// <summary>
    /// The task that is completed, when the process has started, including loading in the main assembly
    /// and CLR.
    /// </summary>
    internal Task Started => this.startedCompletionSource.Task;

    /// <summary>
    /// The metadata reader for the PDB.
    /// </summary>
    internal MetadataReader PdbMetadataReader
    {
        get
        {
            this.pdbMetadataReaderProvider ??= MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(this.PdbPath));
            return this.pdbMetadataReaderProvider.GetMetadataReader();
        }
    }
    private MetadataReaderProvider? pdbMetadataReaderProvider;

    private readonly DebuggerHost host;
    private readonly CorDebug corDebug;
    private readonly CorDebugManagedCallback corDebugManagedCallback;
    private readonly CorDebugProcess corDebugProcess;

    private readonly TaskCompletionSource startedCompletionSource = new();
    private readonly TaskCompletionSource terminatedCompletionSource = new();

    private CorDebugAssembly corDebugAssembly = null!;
    private CorDebugModule corDebugModule = null!;

    internal Debugger(
        DebuggerHost host,
        string programPath,
        CorDebug corDebug,
        CorDebugManagedCallback corDebugManagedCallback,
        CorDebugProcess corDebugProcess)
    {
        this.ProgramPath = programPath;
        this.host = host;
        this.corDebug = corDebug;
        this.corDebugManagedCallback = corDebugManagedCallback;
        this.corDebugProcess = corDebugProcess;

        this.InitializeEventHandler();
    }

    private void InitializeEventHandler()
    {
        this.corDebugManagedCallback.OnAnyEvent += (sender, args) =>
        {
            if (args.Kind == CorDebugManagedCallbackKind.Breakpoint) return;
            this.Resume();
        };
        this.corDebugManagedCallback.OnLoadModule += (sender, args) =>
        {
            Console.WriteLine($"Loaded module {args.Module.Name}");
        };
        var assemblyCount = 0;
        this.corDebugManagedCallback.OnLoadAssembly += (sender, args) =>
        {
            // TODO: Is this reliable?
            if (assemblyCount == 1)
            {
                this.corDebugAssembly = args.Assembly;
                this.corDebugModule = args.Assembly.Modules.Single();

                this.corDebugProcess.Stop(-1);
                this.startedCompletionSource.SetResult();
            }
            ++assemblyCount;
        };
        this.corDebugManagedCallback.OnExitProcess += (sender, args) =>
        {
            this.terminatedCompletionSource.SetResult();
        };
        this.corDebugManagedCallback.OnBreakpoint += (sender, args) =>
        {
            // TODO
            var x = 0;
        };
        this.corDebugManagedCallback.OnUpdateModuleSymbols += (sender, args) =>
        {
            // TODO
            var x = 0;
        };
    }

    /// <summary>
    /// Resumes the execution of the program.
    /// </summary>
    public void Resume() => this.corDebugProcess.TryContinue(false);

    /// <summary>
    /// Sets a breakpoint in the program.
    /// </summary>
    /// <param name="methodDefinitionHandle">The method definition handle index.</param>
    /// <param name="offset">The offset within the method.</param>
    public void SetBreakpoint(int methodDefinitionHandle, int offset)
    {
        var function = this.corDebugModule.GetFunctionFromToken(new mdMethodDef(methodDefinitionHandle));
        var code = function.ILCode;
        var bp = code.CreateBreakpoint(offset);
    }

    /// <summary>
    /// Sets a breakpoint in a given source file, using the given line number.
    /// </summary>
    /// <param name="uri">The URI to the source file.</param>
    /// <param name="lineNumber">The 0-based line number.</param>
    public void SetBreakpoint(Uri uri, int lineNumber)
    {
        if (!this.SourceFiles.TryGetValue(uri, out var file)) return;

        // TODO
        throw new NotImplementedException();
    }

    private ImmutableDictionary<Uri, SourceFile> BuildSourceFiles()
    {
        var reader = this.PdbMetadataReader;
        var result = ImmutableDictionary.CreateBuilder<Uri, SourceFile>();

        foreach (var documentHandle in reader.Documents)
        {
            var document = reader.GetDocument(documentHandle);
            var path = reader.GetString(document.Name);
            var uri = new Uri(path);
            result.Add(uri, new(uri));
        }

        return result.ToImmutable();
    }
}
