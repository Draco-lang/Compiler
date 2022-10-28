using BlazorMonaco;
using Draco.Compiler.Api.Scripting;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.IO.Compression;
using System.Text;
using static ICSharpCode.Decompiler.IL.Transforms.Stepper;

namespace Draco.Editor.Web;

public partial class App
{
    private MonacoEditor? DracoEditor { get; set; }
    private MonacoEditor? OutputViewer { get; set; }
    private string? SelectedOutputType { get; set; } = "Run";
    private static readonly StandaloneEditorConstructionOptions defaultDracoEditorOptions = new()
    {
        AutomaticLayout = true,
        Language = "rust",
        Value = @"func main() {
    println(""Hello!"");
}"
    };

    private static readonly StandaloneEditorConstructionOptions defaultOutputEditorOptions = new()
    {
        AutomaticLayout = true,
        Language = string.Empty,
        Value = string.Empty,
        ReadOnly = true
    };

    private static StandaloneEditorConstructionOptions DracoMonacoOptions(MonacoEditor editor) =>
        defaultDracoEditorOptions;

    private static StandaloneEditorConstructionOptions OutputMonacoOptions(MonacoEditor editor) =>
        defaultOutputEditorOptions;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var isDarkMode = await this.JS.InvokeAsync<bool>("isDarkMode");
        await MonacoEditorBase.SetTheme(isDarkMode ? "vs-dark" : "vs");
        var uriString = this.NavigationManager.Uri;
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
            this.SelectedOutputType = outReader.ReadLine();
            var code = outReader.ReadToEnd();
            await this.DracoEditor!.SetValue(code);
        }
        await this.UpdateOutput();
    }

    private void OnOutputTypeChange(ChangeEventArgs e)
    {
        this.SelectedOutputType = e.Value!.ToString();
        _ = this.UpdateOutput(); // awaiting will block UI thread
    }

    private void CodeChange() => _ = this.UpdateOutput(); // awaiting will block UI thread.

    private async Task UpdateOutput()
    {
        var code = await this.UpdatedURLHash();

        try
        {
            switch (this.SelectedOutputType)
            {
            case "Run":
                await this.ShowRun(code);
                break;
            case "CSharp":
                await this.ShowCSharp(code);
                break;
            case "IL":
                await this.ShowIL(code);
                break;
            default:
                throw new InvalidOperationException();
            }
        }
        catch (Exception e)
        {
            await this.OutputViewer!.SetValue(e.ToString());
        }
    }

    private async Task<string> UpdatedURLHash()
    {
        var code = await this.DracoEditor!.GetValue();
        using var inBuffer = new MemoryStream();
        using var outStream = new DeflateStream(inBuffer, CompressionLevel.Optimal, true);
        using var writer = new StreamWriter(outStream, Encoding.UTF8, leaveOpen: true);
        writer.WriteLine(this.SelectedOutputType);
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
        await this.JS.InvokeVoidAsync("setHash", new object[] { hash });

        return code;
    }

    private async Task ShowRun(string code)
    {
        var oldOut = Console.Out;
        var cts = new CancellationTokenSource();
        var consoleStream = new StringWriter();
        var consoleLoop = this.BackgroundLoop(consoleStream, cts.Token);
        Console.SetOut(consoleStream);
        defaultOutputEditorOptions.Language = string.Empty;
        await this.OutputViewer!.UpdateOptions(defaultOutputEditorOptions);
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
        await this.OutputViewer!.SetValue(consoleStream.ToString());
        Console.SetOut(oldOut);
    }

    private async Task ShowCSharp(string code)
    {
        defaultOutputEditorOptions.Language = "csharp";
        await this.OutputViewer!.UpdateOptions(defaultOutputEditorOptions);
        var cSharpCode = ScriptingEngine.CompileToCSharpCode(code);
        await this.OutputViewer.SetValue(cSharpCode);
    }

    private async Task ShowIL(string code)
    {
        defaultOutputEditorOptions.Language = "IL";
        await this.OutputViewer!.UpdateOptions(defaultOutputEditorOptions);

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
        await this.OutputViewer.SetValue(text.ToString());
    }

    private async Task BackgroundLoop(StringWriter stringWriter, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await this.OutputViewer!.SetValue(stringWriter.ToString());
            try
            {
                await Task.Delay(50, cancellationToken);
            }
            catch (TaskCanceledException) { }
        }
    }
}
