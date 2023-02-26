using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler;
using Microsoft.JSInterop;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using System.Text.Json;
using System.Reflection;
using System.IO;

namespace Draco.Editor.Web;

public partial class Program
{
    private static string code = null!;

    public static void Main() => Interop.Messages += OnMessage;

    public static void OnMessage(string type, string payload)
    {
        switch (type)
        {
        case "CodeChange":
            code = JsonSerializer.Deserialize<string>(payload)!;
            ProcessUserInput();
            return;
        default:
            throw new NotSupportedException();
        }
    }

    private static void ProcessUserInput()
    {
        try
        {
            var tree = SyntaxTree.Parse(code);
            var compilation = Compilation.Create(tree);
            RunScript(compilation);
        }
        catch (Exception e)
        {
            SetOutputText("IL", e.ToString());
            SetOutputText("IR", e.ToString());
            SetOutputText("Run", e.ToString());
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
        return processedAssemblyNames.Where(s => s != assembly).Select(s => $"{s.GetName().Name}.dll")!;
    }


    private static void RunScript(Compilation compilation)
    {
        try
        {
            var dllStream = new MemoryStream();
            var irStream = new MemoryStream();
            var emitResult = compilation.Emit(dllStream, irStream);
            dllStream.Position = 0;
            irStream.Position = 0;
            var hasIR = irStream.Length > 0;
            var hasIL = dllStream.Length > 0;

            if (hasIR)
            {
                var text = new StreamReader(irStream).ReadToEnd();
                SetOutputText("IR", text);
            }

            if (hasIL)
            {
                var text = new PlainTextOutput();
                var disassembler = new ReflectionDisassembler(text, default);
                using (var pe = new PEFile("_", dllStream))
                {
                    disassembler.WriteAssemblyHeader(pe);
                    text.WriteLine();
                    disassembler.WriteModuleContents(pe);
                }
                SetOutputText("IL", text.ToString());
            }

            if (!emitResult.Success)
            {
                dllStream.Dispose();
                var errors = string.Join("\n", emitResult.Diagnostics);
                if (!hasIR) SetOutputText("IR", errors);
                if (!hasIL) SetOutputText("IL", errors);
                SetOutputText("Run", errors);
                return;
            }

            var buffer = dllStream.ToArray();
            var assembly = Assembly.Load(buffer);
            var references = GetAllReferencedAssemblyNames(assembly);
            SendAssembly($"{assembly.GetName().Name!}.dll", buffer, references.ToArray());
        }
        catch (Exception e)
        {
            SetOutputText("Run", e.ToString());
            SetOutputText("IR", e.ToString());
            SetOutputText("IL", e.ToString());
        }
    }

    /// <summary>
    /// Sets the text of the output editor.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static void SetOutputText(string outputType, string text) =>
        Interop.SendMessage("setOutputText", JsonSerializer.Serialize(new
        {
            OutputType = outputType,
            Text = text
        }));

    private static void SendAssembly(string assemblyName, byte[] assembly, string[] referencedAssemblies) =>
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
                }).Append(new
                {
                    behavior = "assembly",
                    name = assemblyName,
                    buffer = (byte[]?)assembly
                }).Append(new
                {
                    behavior = "dotnetwasm",
                    name = "dotnet.wasm",
                    buffer = (byte[]?)default
                }).ToArray()
            }));
}
