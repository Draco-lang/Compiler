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
    private static string code = "";
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        var host = builder.Build();
        js = host.Services.GetRequiredService<IJSRuntime>();
        var navigationManager = host.Services.GetRequiredService<NavigationManager>();
        appJS = await js.InvokeAsync<IJSObjectReference>("import", "/ts/app.ts");
        await GetDataFromURL(navigationManager);
        await appJS.InvokeVoidAsync("emitCodeChange");
        await host.RunAsync();
    }

    private static async Task GetDataFromURL(NavigationManager navigationManager)
    {
        var uriString = navigationManager.Uri;
        var uri = new Uri(uriString);
        if (!string.IsNullOrWhiteSpace(uri.Fragment))
        {
            var strBuffer = new string(uri.Fragment.Skip(1).ToArray());
            // Convert base64URL to Base64.
            // To be replaced with .NET implementation in .NET 8: https://github.com/dotnet/runtime/issues/1658
            strBuffer = Uri
                .UnescapeDataString(strBuffer)
                .Replace('_', '/')
                .Replace('-', '+')
                .PadRight(4 * ((strBuffer.Length + 3) / 4), '='); // Add Base64 padding.
            var buffer = Convert.FromBase64String(strBuffer); // FromBase64 throws when padding is missing.
            using var inBuffer = new MemoryStream(buffer);
            using var gzipStream = new DeflateStream(inBuffer, CompressionMode.Decompress);
            using var outReader = new StreamReader(gzipStream, Encoding.UTF8);
            SelectedOutputType = outReader.ReadLine()!;
            await appJS.InvokeVoidAsync("setRunType", SelectedOutputType);
            code = outReader.ReadToEnd();
            await appJS.InvokeVoidAsync("setEditorText", code);
        }
    }

    private static string SelectedOutputType { get; set; } = "Run";

    [JSInvokable]
    public static async Task OnOutputTypeChange(string value)
    {
        SelectedOutputType = value;
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
        await UpdatedURLHash();

        try
        {
            switch (SelectedOutputType)
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

    private static async Task UpdatedURLHash()
    {
        using var inBuffer = new MemoryStream();
        using var outStream = new DeflateStream(inBuffer, CompressionLevel.Optimal, true);
        using var writer = new StreamWriter(outStream, Encoding.UTF8, leaveOpen: true);
        writer.WriteLine(SelectedOutputType);
        writer.Write(code);
        writer.Flush();
        inBuffer.Position = 0;
        var buffer = inBuffer.ToArray();
        // Convert base64 to base64URL.
        var hash = Convert
            .ToBase64String(buffer)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        await appJS.InvokeVoidAsync("setHash", new object[] { hash });
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


