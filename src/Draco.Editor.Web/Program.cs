using Draco.Compiler.Api.Scripting;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler;
using Microsoft.JSInterop;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using System.Text.Json;
using System.Reflection;

namespace Draco.Editor.Web;

public partial class Program
{
    private static string code = null!;
    private static string selectedOutputType = null!;

    public static void Main() => Interop.Messages += OnMessage;

    public static void OnMessage(string type, string payload)
    {
        switch (type)
        {
        case "OnInit":
            var onInit = (OnInit)JsonSerializer.Deserialize(payload, typeof(OnInit), SourceGenerationContext.Default)!;
            selectedOutputType = onInit.OutputType;
            code = onInit.Code;
            ProcessUserInput();
            return;
        case "OnOutputTypeChange":
            selectedOutputType = JsonSerializer.Deserialize<string>(payload)!;
            ProcessUserInput();
            return;
        case "CodeChange":
            code = JsonSerializer.Deserialize<string>(payload)!;
            ProcessUserInput();
            return;
        default:
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Called when the user changed output type.
    /// </summary>
    /// <param name="value">The new output type.</param>
    public static void OnOutputTypeChange(string value) => selectedOutputType = value;

    /// <summary>
    /// Called when the code input changed.
    /// </summary>
    /// <param name="newCode">The whole code inputed.</param>
    [JSInvokable]
    public static void CodeChange(string newCode)
    {
        code = newCode;
        ProcessUserInput();
    }

    private static void ProcessUserInput()
    {
        try
        {
            var tree = ParseTree.Parse(code);
            var compilation = Compilation.Create(tree);
            switch (selectedOutputType)
            {
            case "Run":
                RunScript(compilation);
                break;
            case "DracoIR":
                DisplayDracoIR(compilation);
                break;
            case "IL":
                var assembly = GetAssemblyStream(compilation);
                if (assembly is null) return;
                DisplayIL(assembly);
                break;
            default:
                throw new InvalidOperationException($"Invalid switch case: {selectedOutputType}.");
            }
        }
        catch (Exception e)
        {
            SetOutputText(e.ToString());
        }
    }

    public static IEnumerable<string> GetAllReferencedAssemblyNames(Assembly assembly)
    {
        var processedAssemblyNames = new HashSet<Assembly>();
        var assembliesToProcess = new Queue<Assembly>();
        assembliesToProcess.Enqueue(assembly);
        while (assembliesToProcess.Any())
        {
            var currentAssembly = assembliesToProcess.Dequeue();
            if (!processedAssemblyNames.Contains(currentAssembly))
            {
                processedAssemblyNames.Add(currentAssembly);
                foreach (var referencedAssembly in currentAssembly.GetReferencedAssemblies())
                {
                    assembliesToProcess.Enqueue(Assembly.Load(referencedAssembly));
                }
            }
        }
        return processedAssemblyNames.Where(s => s != assembly).Select(s => s.GetName().Name+".dll")!;
    }

    private static void RunScript(Compilation compilation)
    {
        try
        {
            var stream = GetAssemblyStream(compilation);
            if (stream is null) return;
            var buffer = stream.ToArray();
            var assembly = Assembly.Load(buffer);
            var references = GetAllReferencedAssemblyNames(assembly);
            SendAssembly(assembly.GetName().Name! + ".dll", buffer, references.ToArray());
        }
        catch (Exception e)
        {
            SetOutputText(e.ToString());
        }
    }

    private static void DisplayDracoIR(Compilation compilation)
    {
        using var irStream = new MemoryStream();
        var emitResult = compilation.Emit(
            peStream: new MemoryStream(),
            dracoIrStream: irStream);
        if (emitResult.Success)
        {
            irStream.Position = 0;
            var text = new StreamReader(irStream).ReadToEnd();
            SetOutputText(text);
        }
        else
        {
            var errors = string.Join("\n", emitResult.Diagnostics);
            SetOutputText(errors);
        }
    }

    private static MemoryStream? GetAssemblyStream(Compilation compilation)
    {
        var inlineDllStream = new MemoryStream();
        var emitResult = compilation.Emit(inlineDllStream);
        if (!emitResult.Success)
        {
            inlineDllStream.Dispose();
            var errors = string.Join("\n", emitResult.Diagnostics);
            SetOutputText(errors);
            return null;
        }
        inlineDllStream.Position = 0;
        return inlineDllStream;
    }

    private static void DisplayIL(MemoryStream inlineDllStream)
    {
        var text = new PlainTextOutput();
        var disassembler = new ReflectionDisassembler(text, default);
        using (var pe = new PEFile("_", inlineDllStream))
        {
            disassembler.WriteAssemblyHeader(pe);
            text.WriteLine();
            disassembler.WriteModuleContents(pe);
        }
        SetOutputText(text.ToString());
    }

    /// <summary>
    /// Sets the text of the output editor.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static void SetOutputText(string text)
    {
        Interop.SendMessage("setOutputText", text);
    }

    private static void SendAssembly(string assemblyName, byte[] assembly, string[] referencedAssemblies)
    {
        Interop.SendMessage("runtimeAssembly",
            JsonSerializer.Serialize(new
            {
                assemblyRootFolder = "_framework", //TODO: this value should be controlled by the JS
                mainAssemblyName = assemblyName,
                assets = referencedAssemblies.Select(s => new
                {
                    behavior = "assembly",
                    name = s,
                    buffer = (byte[]?)default
                }).Append(
                    new
                    {
                        behavior = "assembly",
                        name = assemblyName,
                        buffer = (byte[]?)assembly
                    }
                ).Append(
                    new
                    {
                        behavior = "dotnetwasm",
                        name = "dotnet.wasm",
                        buffer = (byte[]?)default
                    }
                ).ToArray()
            }));
    }
}
