using Draco.Compiler.Api.Scripting;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler;
using Microsoft.JSInterop;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using System.Text.Json;
using ICSharpCode.Decompiler.IL;

namespace Draco.Editor.Web;

public partial class Program
{
    private static string code = null!;
    private static string selectedOutputType = null!;

    public static void Main() => Interop.Messages += OnMessage;


    public static async Task OnMessage(string type, string payload)
    {
        switch (type)
        {
        case "OnInit":
            var onInit = (OnInit)JsonSerializer.Deserialize(payload, typeof(OnInit), SourceGenerationContext.Default)!;
            selectedOutputType = onInit.OutputType;
            code = onInit.Code;
            await ProcessUserInput();
            return;
        case "OnOutputTypeChange":
            selectedOutputType = JsonSerializer.Deserialize<string>(payload)!;
            await ProcessUserInput();
            return;
        case "CodeChange":
            code = JsonSerializer.Deserialize<string>(payload)!;
            await ProcessUserInput();
            return;
        default:
            throw new NotSupportedException();
        }
    }


    /// <summary>
    /// Called when the user changed output type.
    /// </summary>
    /// <param name="value">The new output type.</param>
    public static async Task OnOutputTypeChange(string value) => selectedOutputType = value;

    /// <summary>
    /// Called when the code input changed.
    /// </summary>
    /// <param name="newCode">The whole code inputed.</param>
    [JSInvokable]
    public static async Task CodeChange(string newCode)
    {
        code = newCode;
        await ProcessUserInput();
    }

    private static async Task ProcessUserInput()
    {
        try
        {
            var tree = ParseNode.Parse(code);
            var compilation = Compilation.Create(tree);
            switch (selectedOutputType)
            {
            case "Run":
                await RunScript(compilation);
                break;
            case "CSharp":
                DisplayCompiledCSharp(compilation);
                break;
            case "IL":
                DisplayCompiledIL(compilation);
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

    private static async Task RunScript(Compilation compilation)
    {
        var oldOut = Console.Out;
        var cts = new CancellationTokenSource();
        var consoleStream = new StringWriter();
        var consoleLoop = BackgroundLoop(consoleStream, cts.Token);
        Console.SetOut(consoleStream);
        var outputText = null as string;
        try
        {
            var execResult = ScriptingEngine.Execute(
                compilation,
                csCompilerOptionBuilder: config => config.WithConcurrentBuild(false));
            if (!execResult.Success) outputText = string.Join("\n", execResult.Diagnostics);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        cts.Cancel();
        await consoleLoop;
        outputText ??= consoleStream.ToString();
        SetOutputText(outputText);
        Console.SetOut(oldOut);
    }

    private static void DisplayCompiledCSharp(Compilation compilation)
    {
        var csStream = new MemoryStream();
        var emitResult = compilation.EmitCSharp(csStream);
        if (emitResult.Success)
        {
            csStream.Position = 0;
            var csText = new StreamReader(csStream).ReadToEnd();
            SetOutputText(csText);
        }
        else
        {
            var errors = string.Join("\n", emitResult.Diagnostics);
            SetOutputText(errors);
        }
    }

    private static void DisplayCompiledIL(Compilation compilation)
    {
        using var inlineDllStream = new MemoryStream();
        var emitResult = compilation.Emit(inlineDllStream, csCompilerOptionBuilder: config => config.WithConcurrentBuild(false));
        if (emitResult.Success)
        {
            inlineDllStream.Position = 0;
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
        else
        {
            var errors = string.Join("\n", emitResult.Diagnostics);
            SetOutputText(errors);
        }
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

    /// <summary>
    /// Polling loop that take the script output and display it to the output editor.
    /// </summary>
    /// <returns></returns>
    private static async Task BackgroundLoop(StringWriter stringWriter, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            SetOutputText(stringWriter.ToString());
            try
            {
                await Task.Delay(50, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }
    }
}
