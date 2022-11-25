using Draco.Compiler.Api.Scripting;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Editor.Web;

public class Program
{
    private static IJSRuntime js = null!;
    private static IJSObjectReference appJS = null!;
    private static string code = null!;
    private static string selectedOutputType = null!;

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        var host = builder.Build();
        js = host.Services.GetRequiredService<IJSRuntime>();

        // This will complete when the synchronous part of app.ts finish.
        appJS = await js.InvokeAsync<IJSObjectReference>("import", "./ts/app.js");
        await ProcessUserInput(); // When the app.ts finish, we
        await host.RunAsync();
    }


    [JSInvokable]
    public static void OnInit(string outputType, string newCode)
    {
        // This is run in the typescript init sequence.
        selectedOutputType = outputType;
        code = newCode;
        // We cannot call app.ts here because it has not finished running. The main will do that for us.
    }

    /// <summary>
    /// Called when the user changed output type.
    /// </summary>
    /// <param name="value">The new output type.</param>
    [JSInvokable]
    public static async Task OnOutputTypeChange(string value)
    {
        selectedOutputType = value;
        await ProcessUserInput();
    }

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
            var tree = ParseTree.Parse(code);
            var compilation = Compilation.Create(tree);
            switch (selectedOutputType)
            {
            case "Run":
                await RunScript(compilation);
                break;
            case "CSharp":
                await DisplayCompiledCSharp(compilation);
                break;
            case "IL":
                await DisplayCompiledIL(compilation);
                break;
            default:
                throw new InvalidOperationException("Invalid switch case.");
            }
        }
        catch (Exception e)
        {
            await appJS.InvokeVoidAsync("setOutputText", e.ToString());
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
        await SetOutputText(outputText);
        Console.SetOut(oldOut);
    }

    private static async Task DisplayCompiledCSharp(Compilation compilation)
    {
        var csStream = new MemoryStream();
        var emitResult = compilation.EmitCSharp(csStream);
        if (emitResult.Success)
        {
            csStream.Position = 0;
            var csText = new StreamReader(csStream).ReadToEnd();
            await SetOutputText(csText);
        }
        else
        {
            var errors = string.Join("\n", emitResult.Diagnostics);
            await SetOutputText(errors);
        }
    }

    private static async Task DisplayCompiledIL(Compilation compilation)
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
            await SetOutputText(text.ToString());
        }
        else
        {
            var errors = string.Join("\n", emitResult.Diagnostics);
            await SetOutputText(errors);
        }
    }

    /// <summary>
    /// Sets the text of the output editor.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static async Task SetOutputText(string text) => await appJS.InvokeVoidAsync("setOutputText", text);

    /// <summary>
    /// Polling loop that take the script output and display it to the output editor.
    /// </summary>
    /// <returns></returns>
    private static async Task BackgroundLoop(StringWriter stringWriter, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await SetOutputText(stringWriter.ToString());
            try
            {
                await Task.Delay(50, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }
    }
}
