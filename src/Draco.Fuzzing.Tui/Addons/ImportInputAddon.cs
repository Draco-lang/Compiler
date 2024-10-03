using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;

namespace Draco.Fuzzing.Tui.Addons;

/// <summary>
/// An addon for importing inputs from file.
/// </summary>
/// <typeparam name="TInput">The type of the input model.</typeparam>
public sealed class ImportInputAddon<TInput> : FuzzerAddon
{
    /// <summary>
    /// The file extension of the input files.
    /// </summary>
    public string[]? Extensions { get; set; }

    /// <summary>
    /// The function to parse the input from a string.
    /// </summary>
    public required Func<string, TInput> Parse { get; set; }

    public override MenuBarItem CreateMenuBarItem() =>
        new("Input", [new MenuItem("Import", "Import input files", this.ImportDialog)]);

    /// <summary>
    /// Imports inputs from the specified paths.
    /// </summary>
    /// <param name="paths">The file paths to import.</param>
    public void ImportPaths(IEnumerable<string> paths)
    {
        var inputs = new List<object?>();
        foreach (var path in paths)
        {
            var content = File.ReadAllText(path);
            var input = this.Parse(content);
            inputs.Add(input);
        }
        this.Fuzzer.EnqueueRange(inputs);
    }

    private void ImportDialog()
    {
        var dialog = new OpenDialog("Import", "Select input files")
        {
            AllowsMultipleSelection = true,
            CanChooseFiles = true,
            CanChooseDirectories = false,
            AllowedFileTypes = this.Extensions,
        };

        Terminal.Gui.Application.Run(dialog);

        if (dialog.FilePaths is null) return;
        if (dialog.Canceled) return;

        this.ImportPaths(dialog.FilePaths);
    }
}
