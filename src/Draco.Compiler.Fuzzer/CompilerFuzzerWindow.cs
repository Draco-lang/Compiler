using System.Collections.Generic;
using Draco.Fuzzing.Tui;
using Draco.Fuzzing;
using Terminal.Gui;
using Draco.Fuzzing.Tui.Addons;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Fuzzer;

/// <summary>
/// The fuzzer window for the compiler fuzzer.
/// </summary>
internal sealed class CompilerFuzzerWindow : FuzzerWindow
{
    /// <summary>
    /// Creates the UI for the fuzzer.
    /// </summary>
    /// <param name="fuzzer">The fuzzer to create the UI for.</param>
    /// <returns>The created UI application.</returns>
    public static IFuzzerApplication Create(IFuzzer fuzzer)
    {
        var window = new CompilerFuzzerWindow(fuzzer);

        window.AddAddon(new StartStopAddon());
        window.AddAddon(new ImportInputAddon<SyntaxTree>
        {
            Extensions = [".draco"],
            Parse = text => SyntaxTree.Parse(text),
        });
        window.AddAddon(new InputQueueAddon<SyntaxTree>()
        {
            MaxVisualizedItems = 5000,
        });
        window.AddAddon(new FaultListAddon<SyntaxTree>());
        window.AddAddon(new CoverageScaleAddon());
        window.AddAddon(new CurrentInputAddon<SyntaxTree>());
        window.AddAddon(new MinimizedInputAddon<SyntaxTree>());
        window.AddAddon(new TimingsAddon());
        window.AddAddon(new SeedFooterAddon());
        window.AddAddon(new ExportFaultsAddon<SyntaxTree>());
        window.AddAddon(new ExportFuzzTimesAddon());
        window.AddAddon(new ExportLcovAddon());

        window.Initialize();
        return window;
    }

    private CompilerFuzzerWindow(IFuzzer fuzzer)
        : base(fuzzer)
    {
    }

    protected override IEnumerable<View> Layout(IReadOnlyDictionary<string, View> views)
    {
        var coverageFrame = (FrameView)views["CoverageScale"];
        coverageFrame.Width = Dim.Percent(70);

        var timingsFrame = (FrameView)views["Timings"];
        timingsFrame.X = Pos.Right(coverageFrame);
        timingsFrame.Width = Dim.Fill();

        var currentInputFrame = (FrameView)views["CurrentInput"];
        currentInputFrame.Y = Pos.Bottom(coverageFrame);
        currentInputFrame.Width = Dim.Percent(50);
        currentInputFrame.Height = Dim.Percent(50);

        var minimizedInputFrame = (FrameView)views["MinimizedInput"];
        minimizedInputFrame.Y = Pos.Bottom(coverageFrame);
        minimizedInputFrame.X = Pos.Right(currentInputFrame);
        minimizedInputFrame.Width = Dim.Fill();
        minimizedInputFrame.Height = Dim.Percent(50);

        var inputQueueFrame = (FrameView)views["InputQueue"];
        inputQueueFrame.Y = Pos.Bottom(currentInputFrame);
        inputQueueFrame.Width = Dim.Percent(50);
        inputQueueFrame.Height = Dim.Fill();

        var faultListFrame = (FrameView)views["FaultList"];
        faultListFrame.Y = Pos.Bottom(minimizedInputFrame);
        faultListFrame.X = Pos.Right(inputQueueFrame);
        faultListFrame.Width = Dim.Fill();
        faultListFrame.Height = Dim.Fill();

        return [
            coverageFrame,
            timingsFrame,
            currentInputFrame,
            minimizedInputFrame,
            inputQueueFrame,
            faultListFrame];
    }
}
