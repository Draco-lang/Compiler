using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Terminal.Gui;

namespace Draco.Debugger.Tui;

internal sealed class SourceTextView : TextView
{
    private readonly Terminal.Gui.Attribute highlightedAttribute;
    private List<List<System.Rune>>? lines;
    private SourceRange? highlightedRange;

    public SourceTextView()
    {
        this.highlightedAttribute = Driver.MakeAttribute(Color.White, Color.BrightRed);
        this.TextChanged += this.SourceTextView_TextChanged;
    }

    private void SourceTextView_TextChanged() => this.lines = this.GetLines();

    protected override void SetReadOnlyColor(List<System.Rune> line, int idx)
    {
        Debug.Assert(this.lines is not null);
        var lineIndex = this.lines.IndexOf(line);

        if (this.IsInHighlightedRange(lineIndex, idx))
        {
            Driver.SetAttribute(this.highlightedAttribute);
        }
        else
        {
            base.SetNormalColor(line, idx);
        }
    }

    public void SetHighlightedRange(SourceRange? range)
    {
        this.highlightedRange = range;
        this.SetNeedsDisplay();
    }

    private bool IsInHighlightedRange(int lineIndex, int columnIndex)
    {
        if (this.highlightedRange is null) return false;
        var r = this.highlightedRange.Value;
        var rangeStart = (Line: r.Start.Line, Column: r.Start.Column);
        var rangeEnd = (Line: r.End.Line, Column: r.End.Column);

        var currentPos = (Line: lineIndex, Column: columnIndex);

        return ComparePositions(rangeStart, currentPos) <= 0
            && ComparePositions(currentPos, rangeEnd) < 0;
    }

    private List<List<System.Rune>> GetLines()
    {
        var model = typeof(TextView)
            .GetField("model", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(this)!;
        var lines = model
            .GetType()
            .GetField("lines", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(model)!;
        return (List<List<System.Rune>>)lines;
    }

    private static int ComparePositions((int Line, int Column) p1, (int Line, int Column) p2)
    {
        var lineCmp = p1.Line.CompareTo(p2.Line);
        return lineCmp != 0 ? lineCmp : p1.Column.CompareTo(p2.Column);
    }
}
