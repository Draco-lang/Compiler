using System.IO.Compression;
using System.Text;
using Draco.Compiler.Api.Scripting;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

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
        Console.WriteLine("BeforeImported.");

        appJS = await js.InvokeAsync<IJSObjectReference>("import", "./ts/app.js");
        await UpdateOutput();
        await host.RunAsync();
    }


    [JSInvokable]
    public static void OnInit(string outputType, string newCode)
    {
        selectedOutputType = outputType;
        code = newCode;
    }

    [JSInvokable]
    public static async Task OnOutputTypeChange(string value)
    {
        selectedOutputType = value;
        await UpdateOutput();
    }

    [JSInvokable]
    public static async Task CodeChange(string newCode)
    {
        code = newCode;
        await UpdateOutput();
    }

    private static async Task UpdateOutput()
    {
        try
        {
            switch (selectedOutputType)
            {
            case "Run":
                await ShowRun();
                break;
            case "CSharp":
                await ShowCSharp();
                break;
            case "IL":
                await ShowIL();
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

    private static async Task ShowRun()
    {
        var oldOut = Console.Out;
        var cts = new CancellationTokenSource();
        var consoleStream = new StringWriter();
        var consoleLoop = BackgroundLoop(consoleStream, cts.Token);
        Console.SetOut(consoleStream);
        try
        {
            ScriptingEngine.InlineExecute(code);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        cts.Cancel();
        await consoleLoop;
        await SetOutputText(consoleStream.ToString());
        Console.SetOut(oldOut);
    }

    private static async Task ShowCSharp()
    {
        var cSharpCode = ScriptingEngine.CompileToCSharpCode(code);
        await SetOutputText(cSharpCode);
    }

    private static async Task ShowIL()
    {
        using var inlineDllStream = new MemoryStream();
        if (!ScriptingEngine.CompileToAssembly(code, inlineDllStream)) return;
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

    private static async Task SetOutputText(string text) => await appJS.InvokeVoidAsync("setOutputText", text);

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
