using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

    protected override void SetNormalColor(List<System.Rune> line, int idx)
    {
        base.SetNormalColor(line, idx);
    }

    public void SetHighlightedRange(SourceRange? range) => this.highlightedRange = range;

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
}
