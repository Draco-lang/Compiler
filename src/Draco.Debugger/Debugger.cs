using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using ClrDebug;

namespace Draco.Debugger;

public sealed class Debugger
{
    public Task Terminated => this.terminatedCompletionSource.Task;
    internal Task Started => this.startedCompletionSource.Task;

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
        CorDebug corDebug,
        CorDebugManagedCallback corDebugManagedCallback,
        CorDebugProcess corDebugProcess)
    {
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

    public void Resume() => this.corDebugProcess.TryContinue(false);

    public void SetBreakpoint(int methodDefinitionHandle, int offset)
    {
        var function = this.corDebugModule.GetFunctionFromToken(new mdMethodDef(methodDefinitionHandle));
        var code = function.ILCode;
        var bp = code.CreateBreakpoint(offset);
    }

    public void Foo()
    {
        var module = this.corDebugModule;

        var meta = this.corDebugModule.GetMetaDataInterface();
        var types = meta.MetaDataImport.EnumTypeDefs();
        var typeDef = meta.MetaDataImport.GetTypeDefProps(types[0]);
        var methods = meta.MetaDataImport.EnumMethods(types[0]);
        var method = meta.MetaDataImport.GetMethodProps(methods[0]);
        // var methods = meta.MetaDataImport.EnumMethods();

        var binderRaw = (ISymUnmanagedBinder)new ClrDebug.CoClass.CorSymBinder_SxS();
        var binder = new SymUnmanagedBinder(binderRaw);
        var reader = binder.GetReaderForFile(
            meta.MetaDataImport.Raw,
            "c:\\TMP\\DracoTest\\bin\\Debug\\net7.0\\DracoTest.exe",
            null);
    }
}
