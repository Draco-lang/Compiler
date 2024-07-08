using System;
using System.Collections;
using System.Collections.Generic;
using Terminal.Gui;

namespace Draco.Debugger.Tui;

internal sealed class SourceFileListDataSource(IReadOnlyList<SourceFile> sourceFiles) : IListDataSource
{
    public int Count => this.sourceFiles.Count;
    public int Length => this.sourceFiles.Count;

    private readonly List<SourceFile> sourceFiles = [.. sourceFiles];

    public bool IsMarked(int item) => throw new NotSupportedException();
    public void SetMark(int item, bool value) => throw new NotSupportedException();
    public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
    {
        var selectedFile = this.sourceFiles[item];
        var fileName = selectedFile.Uri.LocalPath;
        driver.AddStr(fileName);
    }
    public IList ToList() => this.sourceFiles;
    public int IndexOf(SourceFile sourceFile) => this.sourceFiles.IndexOf(sourceFile);
}
