using System.IO;
using System;
using Terminal.Gui;
using System.Collections.Generic;
using System.Linq;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon to export faulted inputs.
/// </summary>
/// <typeparam name="TInput">The type of the input model.</typeparam>
public sealed class ExportFaultsAddon<TInput> : FuzzerAddon
{
    public override MenuBarItem CreateMenuBarItem() =>
        new("Faults", [new MenuItem("Export", "Export faulted inputs", this.Export)]);

    private void Export()
    {
        var faultListAddon = this.Application.RequireAddon<FaultListAddon<TInput>>("FaultList", "ExportFaults");
        var faultList = faultListAddon.Faults;

        var dialog = new SaveDialog("Export", "Export the fault list", [".txt"])
        {
            CanCreateDirectories = true,
        };

        Terminal.Gui.Application.Run(dialog);

        if (dialog.Canceled) return;
        if (dialog.FileName is null) return;

        var inputToString = faultListAddon.InputToString ?? InputToStringDefault;
        var faults = $"""
            //////////////////////////////////////////
            // Seed: {this.Fuzzer.Settings.Seed}
            //////////////////////////////////////////

            {string.Join(Environment.NewLine, faultList.Select(f => FormatFaultForExport(inputToString, f)))}
            """;
        File.WriteAllText(dialog.FilePath.ToString()!, faults);
    }

    private static string FormatFaultForExport(Func<TInput, string> inputToString, KeyValuePair<TInput, FaultResult> fault) => $"""
        // ----------------------------------------
        // {FaultListAddon<TInput>.FaultResultToString(fault.Value)}
        // ----------------------------------------
        {inputToString(fault.Key)}
        """;

    private static string InputToStringDefault(TInput input) => input?.ToString() ?? string.Empty;
}
